using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Components;

namespace KK.UI.UMG.Tests
{
    public sealed class UIListViewTests
    {
        [Test]
        public void SetItemsRebuildsItemsAndRaisesItemClicked()
        {
            var root = new GameObject("list");
            try
            {
                var listView = root.AddComponent<UIListView>();
                var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(root.transform, false);
                var template = CreateTemplate(content);
                listView.Configure(
                    content,
                    template,
                    new[]
                    {
                        new UIListView.ItemBinding
                        {
                            ControlId = "Label",
                            ItemField = "name",
                            Property = "text"
                        }
                    },
                    new[]
                    {
                        new UIListView.ItemEvent
                        {
                            ControlId = "ClickButton",
                            Event = "onItemClick",
                            ItemIdField = "id"
                        }
                    });

                var first = new MessagePayload();
                first.Set("id", "item-1");
                first.Set("name", "Item One");
                var second = new MessagePayload();
                second.Set("id", "item-2");
                second.Set("name", "Item Two");

                var clickedIndex = -1;
                var clickedId = string.Empty;
                listView.ItemClicked += (index, itemId) =>
                {
                    clickedIndex = index;
                    clickedId = itemId;
                };

                listView.SetItems(new List<MessagePayload> { first, second });

                Assert.That(content.childCount, Is.EqualTo(3));
                Assert.That(content.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("Item One"));
                Assert.That(content.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("Item Two"));

                content.GetChild(2).Find("ClickButton").GetComponent<Button>().onClick.Invoke();

                Assert.That(clickedIndex, Is.EqualTo(1));
                Assert.That(clickedId, Is.EqualTo("item-2"));

                listView.Clear();

                Assert.That(content.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetItemsExpandsItemWidthFromViewport()
        {
            var root = new GameObject("list");
            try
            {
                var listView = root.AddComponent<UIListView>();
                var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
                viewport.SetParent(root.transform, false);
                viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 320f);
                viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200f);

                var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(viewport, false);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.sizeDelta = Vector2.zero;

                var template = CreateTemplate(content);
                template.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 48f);
                listView.Configure(content, template, new UIListView.ItemBinding[0], new UIListView.ItemEvent[0]);

                var item = new MessagePayload();
                item.Set("id", "item-1");

                listView.SetItems(new List<MessagePayload> { item });

                var instanceRect = content.GetChild(1).GetComponent<RectTransform>();
                Assert.That(instanceRect.rect.width, Is.EqualTo(320f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetItemsReusesItemsAndOverwritesOldState()
        {
            var root = new GameObject("list");
            try
            {
                var listView = root.AddComponent<UIListView>();
                var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(root.transform, false);
                var template = CreateTemplate(content);
                listView.Configure(
                    content,
                    template,
                    new[]
                    {
                        new UIListView.ItemBinding
                        {
                            ControlId = "Label",
                            ItemField = "name",
                            Property = "text"
                        }
                    },
                    new[]
                    {
                        new UIListView.ItemEvent
                        {
                            ControlId = "ClickButton",
                            Event = "onItemClick",
                            ItemIdField = "id"
                        }
                    });

                var first = CreatePayload("item-1", "Item One");
                var second = CreatePayload("item-2", "Item Two");
                listView.SetItems(new List<MessagePayload> { first, second });
                var firstInstance = content.GetChild(1).gameObject;
                var secondInstance = content.GetChild(2).gameObject;

                var clickedCount = 0;
                var clickedId = string.Empty;
                listView.ItemClicked += (index, itemId) =>
                {
                    clickedCount++;
                    clickedId = itemId;
                };

                var third = CreatePayload("item-3", "Item Three");
                listView.SetItems(new List<MessagePayload> { third });
                var reusedInstance = content.GetChild(1).gameObject;

                Assert.That(reusedInstance == firstInstance || reusedInstance == secondInstance, Is.True);
                Assert.That(content.childCount, Is.EqualTo(2));
                Assert.That(content.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("Item Three"));

                content.GetChild(1).Find("ClickButton").GetComponent<Button>().onClick.Invoke();

                Assert.That(clickedCount, Is.EqualTo(1));
                Assert.That(clickedId, Is.EqualTo("item-3"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateTemplate(Transform content)
        {
            var template = new GameObject("ItemTemplate", typeof(RectTransform));
            template.transform.SetParent(content, false);
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(template.transform, false);
            label.AddComponent<TextMeshProUGUI>();
            var button = new GameObject("ClickButton", typeof(RectTransform));
            button.transform.SetParent(template.transform, false);
            button.AddComponent<Button>();
            template.SetActive(false);
            return template;
        }

        private static MessagePayload CreatePayload(string id, string name)
        {
            var payload = new MessagePayload();
            payload.Set("id", id);
            payload.Set("name", name);
            return payload;
        }
    }
}
