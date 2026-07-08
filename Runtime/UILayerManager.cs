using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KK.UI.UMG
{
    internal sealed class UILayerManager
    {
        private readonly LayerStack _layerStack = new LayerStack();
        private int _nextSortingOrder;

        public (string systemId, UIViewBase view) Top => _layerStack.Top;

        public IReadOnlyList<string> Stack => _layerStack.Stack;

        public void Push(string systemId, UIViewBase view)
        {
            _layerStack.Push(systemId, view);
        }

        public void Pop(string systemId)
        {
            _layerStack.Pop(systemId);
        }

        public void AssignSortingOrder(GameObject instance)
        {
            var canvas = instance.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = instance.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = _nextSortingOrder++;

            if (instance.GetComponent<GraphicRaycaster>() == null)
            {
                instance.AddComponent<GraphicRaycaster>();
            }

            if (instance.GetComponent<CanvasGroup>() == null)
            {
                instance.AddComponent<CanvasGroup>();
            }
        }
    }
}
