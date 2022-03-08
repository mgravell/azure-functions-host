// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

//using System;
//using System.Collections.Concurrent;
//using System.Threading.Channels;
//using Microsoft.Azure.WebJobs.Script.Eventing;

//namespace Microsoft.Azure.WebJobs.Script.Grpc.Eventing;

//public sealed class GrpcEventManager : ScriptEventManager
//{
//    private readonly ConcurrentDictionary<string, (Channel<InboundGrpcEvent> Inbound, Channel<OutboundGrpcEvent> Outbound)> workers = new ();

//    private static readonly UnboundedChannelOptions InboundOptions = new UnboundedChannelOptions
//    {
//        SingleReader = true,
//        SingleWriter = false,
//        AllowSynchronousContinuations = true,
//    };

//    private static readonly UnboundedChannelOptions OutboundOptions = new UnboundedChannelOptions
//    {
//        SingleReader = true,
//        SingleWriter = false,
//        AllowSynchronousContinuations = true,
//    };

//    public void AddWorker(string workerId)
//    {
//        var inbound = Channel.CreateUnbounded<InboundGrpcEvent>(InboundOptions);
//        var outbound = Channel.CreateUnbounded<OutboundGrpcEvent>(OutboundOptions);
//        if (!workers.TryAdd(workerId, (inbound, outbound)))
//        {
//            // this is not anticipated, so don't panic abount the allocs above
//            throw new ArgumentException("Duplicate worker id: " + workerId, nameof(workerId));
//        }
//    }

//    public bool TryGetGrpcChannels(string workerId, out Channel<InboundGrpcEvent> inbound, out Channel<OutboundGrpcEvent> outbound)
//    {
//        var result = workers.TryGetValue(workerId, out var pair);
//        inbound = pair.Inbound;
//        outbound = pair.Outbound;
//        return result;
//    }
//}
