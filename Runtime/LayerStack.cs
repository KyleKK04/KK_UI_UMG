using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK.UI.UMG
{
    public sealed class LayerStack
    {
        private readonly Stack<string> _stack = new Stack<string>();
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

                var systemId = _stack.Peek();
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
                    var items = _stack.ToArray();
                    Array.Reverse(items);
                    _cachedStack.AddRange(items);
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

            if (_stack.Count > 0)
            {
                var prevTop = _views[_stack.Peek()];
                prevTop.SetInteraction(false, false);
                prevTop.SetAlpha(DimAlpha);
            }

            _stack.Push(systemId);
            _views[systemId] = view;
            _stackDirty = true;
            view.SetInteraction(true, true);
            view.SetAlpha(1f);
        }

        public void Pop(string systemId)
        {
            if (_stack.Count == 0)
            {
                Debug.LogWarning($"[LayerStack] Pop('{systemId}') ignored: stack is empty.");
                return;
            }

            if (_stack.Peek() != systemId)
            {
                Debug.LogWarning($"[LayerStack] Pop('{systemId}') ignored: top layer is '{_stack.Peek()}'.");
                return;
            }

            _stack.Pop();
            _views.Remove(systemId);
            _stackDirty = true;

            if (_stack.Count == 0)
            {
                return;
            }

            var newTop = _views[_stack.Peek()];
            newTop.SetInteraction(true, true);
            newTop.SetAlpha(1f);
        }
    }
}
