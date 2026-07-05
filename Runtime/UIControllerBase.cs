using System;
using System.Collections.Generic;
using KK.UI.UMG.Binding;

namespace KK.UI.UMG
{
    public abstract class UIControllerBase : IDisposable
    {
        public string SystemId { get; internal set; }
        public UIManager UIManager { get; internal set; }
        public ViewModelStore Store { get; } = new ViewModelStore();
        public UguiBinder Binder { get; } = new UguiBinder();
        public UIViewBase View { get; private set; }
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _disposed;

        public void BindView(UIViewBase view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (View != null)
            {
                throw new InvalidOperationException($"Controller '{GetType().Name}' is already bound to a view.");
            }

            View = view;
            view.Controller = this;
            OnViewBound(view);
        }

        public virtual void Initialize(MessagePayload payload)
        {
        }

        public virtual void Close()
        {
        }

        public virtual void OnPreOpen()
        {
        }

        public virtual void OnOpened()
        {
        }

        public virtual void OnActivated()
        {
        }

        public virtual void OnDeactivated()
        {
        }

        public virtual void OnPreClose()
        {
        }

        public virtual void OnClosed()
        {
        }

        public void Flush()
        {
            Binder.Flush(Store);
        }

        protected T RequireService<T>() where T : class
        {
            if (UIManager == null)
            {
                throw new InvalidOperationException($"Controller '{GetType().Name}' cannot resolve service '{typeof(T).FullName}' because no UIManager is assigned.");
            }

            if (UIManager.TryGetService<T>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException($"Controller '{GetType().Name}' requires service '{typeof(T).FullName}', but it is not registered on UIManager.");
        }

        protected bool TryGetService<T>(out T service) where T : class
        {
            if (UIManager == null)
            {
                service = null;
                return false;
            }

            return UIManager.TryGetService(out service);
        }

        protected IDisposable TrackSubscription(IDisposable subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (_disposed)
            {
                subscription.Dispose();
                throw new ObjectDisposedException(GetType().Name);
            }

            _subscriptions.Add(subscription);
            return subscription;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Dispose(true);
            _disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Exception firstException = null;
            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _subscriptions[i]?.Dispose();
                }
                catch (Exception ex)
                {
                    if (firstException == null)
                    {
                        firstException = ex;
                    }
                }
            }

            _subscriptions.Clear();

            try
            {
                Store.Dispose();
            }
            catch (Exception ex)
            {
                if (firstException == null)
                {
                    firstException = ex;
                }
            }

            if (firstException != null)
            {
                throw firstException;
            }
        }

        protected virtual void OnViewBound(UIViewBase view)
        {
        }

        protected virtual void OnGeneratedInitialize(MessagePayload payload)
        {
        }

        protected virtual void OnGeneratedEvent(string handler, object[] args)
        {
        }
    }
}
