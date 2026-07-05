using System;

namespace KK.UI.UMG
{
    public static class UISubscription
    {
        public static IDisposable Create(Action dispose)
        {
            if (dispose == null)
            {
                throw new ArgumentNullException(nameof(dispose));
            }

            return new DelegateSubscription(dispose);
        }

        private sealed class DelegateSubscription : IDisposable
        {
            private Action _dispose;

            public DelegateSubscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                var dispose = _dispose;
                if (dispose == null)
                {
                    return;
                }

                _dispose = null;
                dispose();
            }
        }
    }
}
