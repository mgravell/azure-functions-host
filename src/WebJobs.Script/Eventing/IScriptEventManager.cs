// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;

namespace Microsoft.Azure.WebJobs.Script.Eventing
{
    public interface IScriptEventManager : IObservable<ScriptEvent>
    {
        void Publish(ScriptEvent scriptEvent);

        bool TryGetDedicatedChannelFor<T>(string workerId, out Channel<T> channel) where T : ScriptEvent;
    }
}
