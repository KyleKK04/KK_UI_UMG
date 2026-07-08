using System;
using System.Collections.Generic;

namespace KK.UI.UMG
{
    internal sealed class UIServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void RegisterService<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var serviceType = typeof(T);
            if (_services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"Service '{serviceType.FullName}' is already registered. Unregister it before registering a replacement.");
            }

            _services.Add(serviceType, service);
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var raw) && raw is T typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public void UnregisterService<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        public void Clear()
        {
            _services.Clear();
        }
    }
}
