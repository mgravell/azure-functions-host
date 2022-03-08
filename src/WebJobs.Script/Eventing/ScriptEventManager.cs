// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading.Channels;

namespace Microsoft.Azure.WebJobs.Script.Eventing
{
    public class ScriptEventManager : IScriptEventManager, IDisposable
    {
        private readonly Subject<ScriptEvent> _subject = new Subject<ScriptEvent>();
        private readonly ConcurrentDictionary<(string, Type), object> _dedicatedChannels = new ();

        private bool _disposed = false;

        private static readonly UnboundedChannelOptions ChannelOptions = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true,
        };

        public void Publish(ScriptEvent scriptEvent)
        {
            ThrowIfDisposed();

            _subject.OnNext(scriptEvent);
        }

        public bool TryGetDedicatedChannelFor<T>(string workerId, out Channel<T> channel) where T : ScriptEvent
        {
            var key = (workerId, typeof(T));
            if (!_dedicatedChannels.TryGetValue(key, out var found))
            {
                found = Channel.CreateUnbounded<T>(ChannelOptions);
                if (!_dedicatedChannels.TryAdd(key, found))
                {
                    found = _dedicatedChannels[key];
                }
            }
            channel = (Channel<T>)found;
            return true;
        }

        public IDisposable Subscribe(IObserver<ScriptEvent> observer)
        {
            ThrowIfDisposed();

            return _subject.Subscribe(observer);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ScriptEventManager));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _subject.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose() => Dispose(true);
    }
}
