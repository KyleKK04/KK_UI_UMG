using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Binding;
using KK.UI.UMG.Components;

namespace KK.UI.UMG.Tests
{
    public sealed class UguiBinderTests
    {
        [Test]
        public void FlushWritesTextBinding()
        {
            var gameObject = new GameObject("view");
            try
            {
                var view = gameObject.AddComponent<TestView>();
                view.MessageText = gameObject.AddComponent<TextMeshProUGUI>();
                var binder = new UguiBinder();
                var store = new ViewModelStore();
                binder.Configure(view, new List<ViewModelBinding>
                {
                    new ViewModelBinding
                    {
                        ControlId = "MessageText",
                        FieldId = "Message",
                        Mode = BindingMode.OneWay,
                        Property = "text"
                    }
                });

                store.Update("Message", "hello world");
                binder.Flush(store);

                Assert.That(view.MessageText.text, Is.EqualTo("hello world"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FlushWritesCommonV052Controls()
        {
            var gameObject = new GameObject("view");
            try
            {
                var view = gameObject.AddComponent<TestView>();
                view.EnabledToggle = AddChildComponent<Toggle>(gameObject, "EnabledToggle");
                view.VolumeSlider = AddChildComponent<Slider>(gameObject, "VolumeSlider");
                view.NameInput = AddChildComponent<TMP_InputField>(gameObject, "NameInput");
                view.ModeDropdown = AddChildComponent<TMP_Dropdown>(gameObject, "ModeDropdown");
                view.ModeDropdown.options.Add(new TMP_Dropdown.OptionData("A"));
                view.ModeDropdown.options.Add(new TMP_Dropdown.OptionData("B"));
                view.List = AddChildComponent<UIListView>(gameObject, "List");
                var binder = new UguiBinder();
                var store = new ViewModelStore();
                binder.Configure(view, new List<ViewModelBinding>
                {
                    new ViewModelBinding { ControlId = "EnabledToggle", FieldId = "Enabled", Mode = BindingMode.OneWay, Property = "isOn" },
                    new ViewModelBinding { ControlId = "VolumeSlider", FieldId = "Volume", Mode = BindingMode.OneWay, Property = "value" },
                    new ViewModelBinding { ControlId = "NameInput", FieldId = "Name", Mode = BindingMode.OneWay, Property = "text" },
                    new ViewModelBinding { ControlId = "ModeDropdown", FieldId = "Mode", Mode = BindingMode.OneWay, Property = "value" },
                    new ViewModelBinding { ControlId = "List", FieldId = "Items", Mode = BindingMode.OneWay, Property = "items" }
                });

                var items = new List<MessagePayload>();
                store.Update("Enabled", true);
                store.Update("Volume", 0.75f);
                store.Update("Name", "Player");
                store.Update("Mode", 1);
                store.Update<IReadOnlyList<MessagePayload>>("Items", items);
                binder.Flush(store);

                Assert.That(view.EnabledToggle.isOn, Is.True);
                Assert.That(view.VolumeSlider.value, Is.EqualTo(0.75f));
                Assert.That(view.NameInput.text, Is.EqualTo("Player"));
                Assert.That(view.ModeDropdown.value, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FlushWritesImageColorAndAlphaBindings()
        {
            var gameObject = new GameObject("view");
            try
            {
                var view = gameObject.AddComponent<TestView>();
                view.StatusImage = AddChildComponent<Image>(gameObject, "StatusImage");
                view.StatusImage.color = Color.white;
                var binder = new UguiBinder();
                var store = new ViewModelStore();
                binder.Configure(view, new List<ViewModelBinding>
                {
                    new ViewModelBinding { ControlId = "StatusImage", FieldId = "StatusColor", Mode = BindingMode.OneWay, Property = "color" },
                    new ViewModelBinding { ControlId = "StatusImage", FieldId = "StatusAlpha", Mode = BindingMode.OneWay, Property = "alpha" },
                    new ViewModelBinding { ControlId = "StatusImage", FieldId = "StatusFill", Mode = BindingMode.OneWay, Property = "fillAmount" },
                    new ViewModelBinding { ControlId = "StatusImage", FieldId = "StatusRaycast", Mode = BindingMode.OneWay, Property = "raycastTarget" }
                });

                store.Update("StatusColor", new Color(0.25f, 0.5f, 0.75f, 1f));
                store.Update("StatusAlpha", 0.4f);
                store.Update("StatusFill", 0.6f);
                store.Update("StatusRaycast", false);
                binder.Flush(store);

                Assert.That(view.StatusImage.color.r, Is.EqualTo(0.25f).Within(0.001f));
                Assert.That(view.StatusImage.color.g, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(view.StatusImage.color.b, Is.EqualTo(0.75f).Within(0.001f));
                Assert.That(view.StatusImage.color.a, Is.EqualTo(0.4f).Within(0.001f));
                Assert.That(view.StatusImage.fillAmount, Is.EqualTo(0.6f).Within(0.001f));
                Assert.That(view.StatusImage.raycastTarget, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FlushParsesImageColorStringBinding()
        {
            var gameObject = new GameObject("view");
            try
            {
                var view = gameObject.AddComponent<TestView>();
                view.StatusImage = AddChildComponent<Image>(gameObject, "StatusImage");
                var binder = new UguiBinder();
                var store = new ViewModelStore();
                binder.Configure(view, new List<ViewModelBinding>
                {
                    new ViewModelBinding { ControlId = "StatusImage", FieldId = "StatusColor", Mode = BindingMode.OneWay, Property = "color" }
                });

                store.Update("StatusColor", "#33669980");
                binder.Flush(store);

                Assert.That(view.StatusImage.color.r, Is.EqualTo(0.2f).Within(0.01f));
                Assert.That(view.StatusImage.color.g, Is.EqualTo(0.4f).Within(0.01f));
                Assert.That(view.StatusImage.color.b, Is.EqualTo(0.6f).Within(0.01f));
                Assert.That(view.StatusImage.color.a, Is.EqualTo(0.5f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FlushWritesCommonGraphicProperties()
        {
            var gameObject = new GameObject("view");
            try
            {
                var view = gameObject.AddComponent<TestView>();
                view.MessageText = AddChildComponent<TextMeshProUGUI>(gameObject, "MessageText");
                view.PreviewImage = AddChildComponent<RawImage>(gameObject, "PreviewImage");
                view.ActionButton = AddChildComponent<Button>(gameObject, "ActionButton");
                view.ActionButton.targetGraphic = view.ActionButton.gameObject.AddComponent<Image>();
                var panelObject = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(gameObject.transform, false);
                view.PanelRoot = panelObject.GetComponent<RectTransform>();
                var panelImage = panelObject.GetComponent<Image>();
                var binder = new UguiBinder();
                var store = new ViewModelStore();
                binder.Configure(view, new List<ViewModelBinding>
                {
                    new ViewModelBinding { ControlId = "MessageText", FieldId = "MessageColor", Mode = BindingMode.OneWay, Property = "color" },
                    new ViewModelBinding { ControlId = "MessageText", FieldId = "MessageSize", Mode = BindingMode.OneWay, Property = "fontSize" },
                    new ViewModelBinding { ControlId = "PreviewImage", FieldId = "PreviewAlpha", Mode = BindingMode.OneWay, Property = "alpha" },
                    new ViewModelBinding { ControlId = "ActionButton", FieldId = "ButtonColor", Mode = BindingMode.OneWay, Property = "color" },
                    new ViewModelBinding { ControlId = "PanelRoot", FieldId = "PanelAlpha", Mode = BindingMode.OneWay, Property = "alpha" }
                });

                store.Update("MessageColor", "#FFCC00");
                store.Update("MessageSize", 32);
                store.Update("PreviewAlpha", 0.25f);
                store.Update("ButtonColor", new Color(0.1f, 0.2f, 0.3f, 0.4f));
                store.Update("PanelAlpha", 0.35f);
                binder.Flush(store);

                Assert.That(view.MessageText.color.r, Is.EqualTo(1f).Within(0.01f));
                Assert.That(view.MessageText.color.g, Is.EqualTo(0.8f).Within(0.01f));
                Assert.That(view.MessageText.fontSize, Is.EqualTo(32f).Within(0.001f));
                Assert.That(view.PreviewImage.color.a, Is.EqualTo(0.25f).Within(0.001f));
                Assert.That(view.ActionButton.targetGraphic.color.r, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(view.ActionButton.targetGraphic.color.a, Is.EqualTo(0.4f).Within(0.001f));
                Assert.That(panelImage.color.a, Is.EqualTo(0.35f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class TestView : UIViewBase
        {
            public TextMeshProUGUI MessageText { get; set; }
            public Toggle EnabledToggle { get; set; }
            public Slider VolumeSlider { get; set; }
            public TMP_InputField NameInput { get; set; }
            public TMP_Dropdown ModeDropdown { get; set; }
            public UIListView List { get; set; }
            public Image StatusImage { get; set; }
            public RawImage PreviewImage { get; set; }
            public Button ActionButton { get; set; }
            public RectTransform PanelRoot { get; set; }

            protected override void BindEvents()
            {
            }

            protected override void UnbindEvents()
            {
            }
        }

        private static T AddChildComponent<T>(GameObject root, string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<T>();
        }
    }
}
