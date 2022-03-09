// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.ManagedDependencies;

namespace Microsoft.Azure.WebJobs.Script.Workers.Rpc
{
    public interface IRpcWorkerChannel : IWorkerChannel
    {
        bool TryPost(string functionId, ScriptInvocationContext ctx);

        bool IsChannelReadyForInvocations();

        void SetupFunctionInvocationBuffers(IEnumerable<FunctionMetadata> functions);

        void SendFunctionLoadRequests(ManagedDependencyOptions managedDependencyOptions, TimeSpan? functionTimeout);

        Task SendFunctionEnvironmentReloadRequest();

        Task<List<RawFunctionMetadata>> GetFunctionMetadata();

        Task DrainInvocationsAsync();

        bool IsExecutingInvocation(string invocationId);

        bool TryFailExecutions(Exception workerException);
    }
}
