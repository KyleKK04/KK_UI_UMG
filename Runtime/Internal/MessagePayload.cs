using System;
using System.Collections.Generic;

namespace KK.UI.UMG
{
    public sealed class MessagePayload
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Payload key cannot be null or empty.", nameof(key));
            }

            _values[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (!_values.TryGetValue(key, out var raw))
            {
                value = default;
                return false;
            }

            if (raw == null)
            {
                value = default;
                return !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null;
            }

            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public T Get<T>(string key)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Payload does not contain key '{key}' with type '{typeof(T).Name}'.");
        }
    }
}
