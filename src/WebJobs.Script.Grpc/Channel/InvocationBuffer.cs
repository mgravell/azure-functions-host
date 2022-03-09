// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Grpc
{
    internal sealed class InvocationBuffer
    {
        private readonly ConcurrentQueue<ScriptInvocationContext> _queue = new ();

        public void Post(ScriptInvocationContext ctx)
            => _queue.Enqueue(ctx);

        public void Flush(GrpcWorkerChannel channel)
        {
            while (_queue.TryDequeue(out var ctx))
            {
                _ = channel.SendInvocationRequest(ctx);
            }
        }
    }
}
