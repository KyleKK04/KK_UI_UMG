using System.Collections.Generic;
using UnityEngine;

namespace KK.UI.UMG.Components
{
    internal sealed class UIObjectPool
    {
        private readonly GameObject _template;
        private readonly Transform _poolRoot;
        private readonly Stack<GameObject> _items = new Stack<GameObject>();

        public UIObjectPool(GameObject template, Transform poolRoot)
        {
            _template = template;
            _poolRoot = poolRoot;
        }

        public GameObject Get(Transform parent)
        {
            GameObject item;
            if (_items.Count > 0)
            {
                item = _items.Pop();
                item.transform.SetParent(parent, false);
            }
            else
            {
                item = Object.Instantiate(_template, parent, false);
            }

            item.SetActive(true);
            return item;
        }

        public void Release(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            item.SetActive(false);
            item.transform.SetParent(_poolRoot, false);
            _items.Push(item);
        }

        public void DestroyAll()
        {
            while (_items.Count > 0)
            {
                DestroyObject(_items.Pop());
            }
        }

        private static void DestroyObject(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(item);
            }
            else
            {
                Object.DestroyImmediate(item);
            }
        }
    }
}
