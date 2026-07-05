using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG;

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

        public event Action<int, string> ItemClicked;

        public void Configure(RectTransform content, GameObject itemTemplate, IReadOnlyList<ItemBinding> itemBindings, IReadOnlyList<ItemEvent> itemEvents)
        {
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
            Clear();
            if (_content == null || _itemTemplate == null || items == null)
            {
                return;
            }

            NormalizeContentRect();
            for (var i = 0; i < items.Count; i++)
            {
                var payload = items[i];
                var item = Instantiate(_itemTemplate, _content, false);
                item.name = $"{_itemTemplate.name}_{i}";
                NormalizeItemRect(item);
                item.SetActive(true);
                ApplyBindings(item.transform, payload);
                BindItemEvents(item.transform, payload, i);
                _instances.Add(item);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        public void Clear()
        {
            foreach (var instance in _instances)
            {
                if (instance == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(instance);
                }
                else
                {
                    DestroyImmediate(instance);
                }
            }

            _instances.Clear();
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

                Apply(target, binding.Property, value);
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

        private static void Apply(Transform target, string property, object value)
        {
            if (target.TryGetComponent<TMP_Text>(out var text) && property == "text")
            {
                text.text = value?.ToString() ?? string.Empty;
                return;
            }

            if (target.TryGetComponent<Image>(out var image) && property == "sprite")
            {
                image.sprite = value as Sprite;
                return;
            }

            if (target.TryGetComponent<RawImage>(out var rawImage) && property == "texture")
            {
                rawImage.texture = value as Texture;
                return;
            }

            if (target.TryGetComponent<Button>(out var button) && property == "interactable")
            {
                button.interactable = value is bool enabled && enabled;
            }
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var found = FindByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
