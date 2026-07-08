using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG;
using KK.UI.UMG.Binding;

namespace KK.UI.UMG.Components
{
    public sealed class UIListView : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemBinding
        {
            public string ControlId;
            public string ItemField;
            public string Property;
        }

        [Serializable]
        public sealed class ItemEvent
        {
            public string ControlId;
            public string Event;
            public string Handler;
            public string ItemIdField;
        }

        [SerializeField] private RectTransform _content;
        [SerializeField] private GameObject _itemTemplate;
        [SerializeField] private List<ItemBinding> _itemBindings = new List<ItemBinding>();
        [SerializeField] private List<ItemEvent> _itemEvents = new List<ItemEvent>();

        private readonly List<GameObject> _instances = new List<GameObject>();
        private readonly Dictionary<GameObject, Dictionary<string, Transform>> _lookupCache = new Dictionary<GameObject, Dictionary<string, Transform>>();
        private UIObjectPool _pool;
        private Transform _poolRoot;

        public event Action<int, string> ItemClicked;

        public void Configure(RectTransform content, GameObject itemTemplate, IReadOnlyList<ItemBinding> itemBindings, IReadOnlyList<ItemEvent> itemEvents)
        {
            ReleaseActiveItems();
            DestroyPool();

            _content = content;
            _itemTemplate = itemTemplate;
            _itemBindings = itemBindings == null ? new List<ItemBinding>() : new List<ItemBinding>(itemBindings);
            _itemEvents = itemEvents == null ? new List<ItemEvent>() : new List<ItemEvent>(itemEvents);

            if (_itemTemplate != null)
            {
                _itemTemplate.SetActive(false);
            }
        }

        public void SetItems(IReadOnlyList<MessagePayload> items)
        {
            ReleaseActiveItems();
            if (_content == null || _itemTemplate == null || items == null)
            {
                return;
            }

            NormalizeContentRect();
            EnsurePool();
            for (var i = 0; i < items.Count; i++)
            {
                var payload = items[i];
                var item = _pool.Get(_content);
                item.name = $"{_itemTemplate.name}_{i}";
                NormalizeItemRect(item);
                ResetItem(item.transform);
                ApplyBindings(item.transform, payload);
                BindItemEvents(item.transform, payload, i);
                _instances.Add(item);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        public void Clear()
        {
            ReleaseActiveItems();
        }

        private void OnDestroy()
        {
            ReleaseActiveItems();
            DestroyPool();
        }

        private void ReleaseActiveItems()
        {
            EnsurePool();
            foreach (var instance in _instances)
            {
                if (instance == null)
                {
                    continue;
                }

                if (_pool == null)
                {
                    DestroyInstance(instance);
                    continue;
                }

                _pool.Release(instance);
            }

            _instances.Clear();
        }

        private void EnsurePool()
        {
            if (_pool != null || _itemTemplate == null)
            {
                return;
            }

            if (_poolRoot == null)
            {
                var poolObject = new GameObject($"{name}_PooledItems", typeof(RectTransform));
                poolObject.transform.SetParent(transform, false);
                poolObject.SetActive(false);
                _poolRoot = poolObject.transform;
            }

            _pool = new UIObjectPool(_itemTemplate, _poolRoot);
        }

        private void DestroyPool()
        {
            _pool?.DestroyAll();
            _pool = null;
            _lookupCache.Clear();

            if (_poolRoot == null)
            {
                return;
            }

            var poolObject = _poolRoot.gameObject;
            _poolRoot = null;
            if (Application.isPlaying)
            {
                Destroy(poolObject);
            }
            else
            {
                DestroyImmediate(poolObject);
            }
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }

        private void ResetItem(Transform itemRoot)
        {
            foreach (var button in itemRoot.GetComponentsInChildren<Button>(true))
            {
                button.onClick.RemoveAllListeners();
            }

            foreach (var binding in _itemBindings)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.ControlId))
                {
                    continue;
                }

                var target = FindByName(itemRoot, binding.ControlId);
                if (target == null)
                {
                    continue;
                }

                UguiApplyHelper.TryApply(target, binding.Property, GetDefaultValue(binding.Property));
            }
        }

        private void ApplyBindings(Transform itemRoot, MessagePayload payload)
        {
            foreach (var binding in _itemBindings)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.ControlId) || string.IsNullOrWhiteSpace(binding.ItemField))
                {
                    continue;
                }

                if (!payload.TryGet<object>(binding.ItemField, out var value))
                {
                    continue;
                }

                var target = FindByName(itemRoot, binding.ControlId);
                if (target == null)
                {
                    continue;
                }

                UguiApplyHelper.TryApply(target, binding.Property, value);
            }
        }

        private void BindItemEvents(Transform itemRoot, MessagePayload payload, int index)
        {
            foreach (var itemEvent in _itemEvents)
            {
                if (itemEvent == null || itemEvent.Event != "onItemClick")
                {
                    continue;
                }

                var target = FindByName(itemRoot, itemEvent.ControlId);
                var button = target == null ? null : target.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                var itemId = string.Empty;
                if (!string.IsNullOrWhiteSpace(itemEvent.ItemIdField))
                {
                    payload.TryGet(itemEvent.ItemIdField, out itemId);
                }

                button.onClick.AddListener(() => ItemClicked?.Invoke(index, itemId));
            }
        }

        private void NormalizeContentRect()
        {
            if (!(_content.parent is RectTransform viewport))
            {
                return;
            }

            _content.anchorMin = new Vector2(0f, _content.anchorMin.y);
            _content.anchorMax = new Vector2(1f, _content.anchorMax.y);
            _content.anchoredPosition = new Vector2(0f, _content.anchoredPosition.y);
            _content.sizeDelta = new Vector2(0f, _content.sizeDelta.y);

            if (viewport.rect.width > 0f)
            {
                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewport.rect.width);
            }
        }

        private void NormalizeItemRect(GameObject item)
        {
            var rect = item == null ? null : item.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);

            var width = _content.rect.width;
            if (width <= 0f && _content.parent is RectTransform viewport)
            {
                width = viewport.rect.width;
            }

            if (width > 0f)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
        }

        private Transform FindByName(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (!_lookupCache.TryGetValue(root.gameObject, out var cache))
            {
                cache = new Dictionary<string, Transform>();
                _lookupCache[root.gameObject] = cache;
            }

            if (cache.TryGetValue(name, out var cached) && cached != null)
            {
                return cached;
            }

            var found = FindByNameRecursive(root, name);
            cache[name] = found;
            return found;
        }

        private static Transform FindByNameRecursive(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var found = FindByNameRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static object GetDefaultValue(string property)
        {
            if (string.Equals(property, "text", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (string.Equals(property, "sprite", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property, "texture", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.Equals(property, "interactable", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(property, "isOn", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(property, "value", StringComparison.OrdinalIgnoreCase))
            {
                return 0f;
            }

            return null;
        }
    }
}
