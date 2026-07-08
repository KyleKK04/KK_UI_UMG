using System;
using System.Collections.Generic;

namespace KK.UI.UMG
{
    public readonly struct ViewModelDirtyField
    {
        public ViewModelDirtyField(string fieldId, object value)
        {
            FieldId = fieldId;
            Value = value;
        }

        public string FieldId { get; }
        public object Value { get; }
    }

    public sealed class ViewModelStore : IDisposable
    {
        private readonly Dictionary<string, object> _fields = new Dictionary<string, object>();
        private readonly HashSet<string> _dirty = new HashSet<string>();
        private readonly List<ViewModelDirtyField> _dirtyBuffer = new List<ViewModelDirtyField>();
        private bool _disposed;

        public void Update<T>(string fieldId, T value)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(fieldId))
            {
                throw new ArgumentException("ViewModel field id cannot be null or empty.", nameof(fieldId));
            }

            _fields[fieldId] = value;
            _dirty.Add(fieldId);
        }

        public bool TryGet<T>(string fieldId, out T value)
        {
            ThrowIfDisposed();
            if (_fields.TryGetValue(fieldId, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public T Get<T>(string fieldId)
        {
            ThrowIfDisposed();
            if (TryGet<T>(fieldId, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"ViewModel field '{fieldId}' is missing or not a {typeof(T).Name}.");
        }

        public IReadOnlyList<ViewModelDirtyField> TakeDirty()
        {
            ThrowIfDisposed();
            _dirtyBuffer.Clear();
            foreach (var fieldId in _dirty)
            {
                _fields.TryGetValue(fieldId, out var value);
                _dirtyBuffer.Add(new ViewModelDirtyField(fieldId, value));
            }

            _dirty.Clear();
            return _dirtyBuffer;
        }

        public void Clear()
        {
            ThrowIfDisposed();
            _fields.Clear();
            _dirty.Clear();
            _dirtyBuffer.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _fields.Clear();
            _dirty.Clear();
            _dirtyBuffer.Clear();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ViewModelStore));
            }
        }
    }
}
