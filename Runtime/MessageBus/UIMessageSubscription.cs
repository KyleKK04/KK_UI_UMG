using System;

namespace KK.UI.UMG.MessageBus
{
    internal sealed class UIMessageSubscription : IDisposable
    {
        private string _channel;
        private Action<string, MessagePayload> _handler;
        private readonly Action<string, Action<string, MessagePayload>> _unsubscribe;

        public UIMessageSubscription(
            string channel,
            Action<string, MessagePayload> handler,
            Action<string, Action<string, MessagePayload>> unsubscribe)
        {
            _channel = channel;
            _handler = handler;
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (_handler == null)
            {
                return;
            }

            _unsubscribe?.Invoke(_channel, _handler);
            _channel = null;
            _handler = null;
        }
    }
}
