// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Eventing;
using Microsoft.Azure.WebJobs.Script.Grpc.Eventing;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Microsoft.Azure.WebJobs.Script.Workers.Http;
using Microsoft.Extensions.Logging;

using MsgType = Microsoft.Azure.WebJobs.Script.Grpc.Messages.StreamingMessage.ContentOneofCase;

namespace Microsoft.Azure.WebJobs.Script.Grpc
{
    // Implementation for the grpc service
    // TODO: move to WebJobs.Script.Grpc package and provide event stream abstraction
    internal class FunctionRpcService : FunctionRpc.FunctionRpcBase
    {
        private readonly IScriptEventManager _eventManager;
        private readonly ILogger _logger;

        public FunctionRpcService(IScriptEventManager eventManager, ILogger<FunctionRpcService> logger)
        {
            _eventManager = eventManager;
            _logger = logger;
        }

        public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream, IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
        {
            var cancelSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            CancellationTokenRegistration ctr = cts.Token.Register(static state => ((TaskCompletionSource<bool>)state).TrySetResult(false), cancelSource);
            try
            {
                static Task<Task<bool>> MoveNextAsync(IAsyncStreamReader<StreamingMessage> requestStream, TaskCompletionSource<bool> cancelSource)
                {
                    // GRPC does not accept cancellation tokens for individual reads, hence wrapper
                    var requestTask = requestStream.MoveNext(CancellationToken.None);
                    return Task.WhenAny(cancelSource.Task, requestTask);
                }

                if (await await MoveNextAsync(requestStream, cancelSource))
                {
                    var currentMessage = requestStream.Current;
                    // expect first operation (and only the first; we don't support re-registration) to be StartStream
                    if (currentMessage.ContentCase == MsgType.StartStream)
                    {
                        var workerId = currentMessage.StartStream?.WorkerId;
                        currentMessage = null;
                        if (string.IsNullOrEmpty(workerId))
                        {
                            if (_eventManager.TryGetDedicatedChannelFor<OutboundGrpcEvent>(workerId, out var outbound))
                            {
                                // register this worker and listen for replies
                                _ = RegisterWorker(workerId, responseStream, outbound.Reader, cts.Token);
                            }

                            if (_eventManager.TryGetDedicatedChannelFor<InboundGrpcEvent>(workerId, out var inbound))
                            {
                                while (await await MoveNextAsync(requestStream, cancelSource))
                                {
                                    currentMessage = requestStream.Current;
                                    if (currentMessage.ContentCase == MsgType.InvocationResponse && !string.IsNullOrEmpty(currentMessage.InvocationResponse?.InvocationId))
                                    {
                                        _logger.LogTrace("Received invocation response for invocationId: {invocationId} from workerId: {workerId}", currentMessage.InvocationResponse.InvocationId, workerId);
                                    }
                                    var newInbound = new InboundGrpcEvent(workerId, currentMessage);
                                    if (!inbound.Writer.TryWrite(newInbound))
                                    {
                                        await inbound.Writer.WriteAsync(newInbound);
                                    }
                                    currentMessage = null; // allow old messages to be collected while we wait
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception rpcException)
            {
                // We catch the exception, just to report it, then re-throw it
                _logger.LogError(rpcException, "Exception encountered while listening to EventStream");
                throw;
            }
            finally
            {
                cts.Cancel();
                ctr.Dispose();

                // ensure cancellationSource task completes
                cancelSource.TrySetResult(false);
            }
        }

        private async Task RegisterWorker(string workerId, IServerStreamWriter<StreamingMessage> responseStream, ChannelReader<OutboundGrpcEvent> source, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Established RPC channel. WorkerId: {workerId}", workerId);

            try
            {
                await Task.Yield(); // free up the caller
                while (await source.WaitToReadAsync(cancellationToken))
                {
                    while (source.TryRead(out var evt))
                    {
                        if (evt.MessageType == MsgType.InvocationRequest)
                        {
                            _logger.LogTrace("Writing invocation request invocationId: {invocationId} to workerId: {workerId}", evt.Message.InvocationRequest.InvocationId, workerId);
                        }
                        try
                        {
                            await responseStream.WriteAsync(evt.Message);
                        }
                        catch (Exception subscribeEventEx)
                        {
                            _logger.LogError(subscribeEventEx, "Error writing message type {messageType} to workerId: {workerId}", evt.MessageType, workerId);
                        }
                    }
                }
            }
            catch
            {
                // do something
            }
        }
    }
}
