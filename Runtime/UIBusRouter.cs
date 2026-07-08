using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KK.UI.UMG.Internal;
using KK.UI.UMG.MessageBus;

namespace KK.UI.UMG
{
    internal sealed class UIBusRouter : IDisposable
    {
        private readonly Func<string, MessagePayload, Task<UIViewBase>> _openAsync;
        private readonly Func<string, Task> _closeAsync;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public UIBusRouter(Func<string, MessagePayload, Task<UIViewBase>> openAsync, Func<string, Task> closeAsync)
        {
            _openAsync = openAsync ?? throw new ArgumentNullException(nameof(openAsync));
            _closeAsync = closeAsync ?? throw new ArgumentNullException(nameof(closeAsync));
        }

        public void Subscribe(IEnumerable<UIBusRoute> routes)
        {
            Dispose();
            foreach (var route in routes)
            {
                var routeCopy = route;
                var subscription = UIMessageBus.Subscribe(routeCopy.Channel, (channel, payload) =>
                {
                    if (routeCopy.Action == UIBusRouteAction.Open)
                    {
                        _ = _openAsync(routeCopy.SystemId, payload ?? new MessagePayload());
                        return;
                    }

                    if (routeCopy.Action == UIBusRouteAction.Close)
                    {
                        _ = _closeAsync(routeCopy.SystemId);
                    }
                });

                _subscriptions.Add(subscription);
            }
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }
}
