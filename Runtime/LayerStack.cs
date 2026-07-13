using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK.UI.UMG
{
    public sealed class LayerStack
    {
        private readonly List<string> _stack = new List<string>();
        private readonly Dictionary<string, UIViewBase> _views = new Dictionary<string, UIViewBase>();
        private readonly List<string> _cachedStack = new List<string>();
        private bool _stackDirty = true;

        public float DimAlpha { get; set; } = 1f;

        public (string systemId, UIViewBase view) Top
        {
            get
            {
                if (_stack.Count == 0)
                {
                    return (null, null);
                }

                var systemId = _stack[_stack.Count - 1];
                return (systemId, _views.TryGetValue(systemId, out var view) ? view : null);
            }
        }

        public IReadOnlyList<string> Stack
        {
            get
            {
                if (_stackDirty)
                {
                    _cachedStack.Clear();
                    _cachedStack.AddRange(_stack);
                    _stackDirty = false;
                }

                return _cachedStack;
            }
        }

        public void Push(string systemId, UIViewBase view)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("UI system id cannot be null or empty.", nameof(systemId));
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            var existingIndex = _stack.IndexOf(systemId);
            if (existingIndex >= 0)
            {
                _stack.RemoveAt(existingIndex);
            }

            _stack.Add(systemId);
            _views[systemId] = view;
            _stackDirty = true;
        }

        public void Pop(string systemId)
        {
            if (_stack.Count == 0)
            {
                Debug.LogWarning($"[LayerStack] Pop('{systemId}') ignored: stack is empty.");
                return;
            }

            if (!IsTop(systemId))
            {
                Debug.LogWarning($"[LayerStack] Pop('{systemId}') ignored: top layer is '{_stack[_stack.Count - 1]}'.");
                return;
            }

            Remove(systemId);
        }

        public bool Remove(string systemId)
        {
            var index = _stack.IndexOf(systemId);
            if (index < 0)
            {
                return false;
            }

            _stack.RemoveAt(index);
            _views.Remove(systemId);
            _stackDirty = true;
            return true;
        }

        public bool IsTop(string systemId)
        {
            return _stack.Count > 0 && _stack[_stack.Count - 1] == systemId;
        }
    }
}
