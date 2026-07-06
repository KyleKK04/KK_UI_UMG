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

        private sealed class TestView : UIViewBase
        {
            public TextMeshProUGUI MessageText { get; set; }
            public Toggle EnabledToggle { get; set; }
            public Slider VolumeSlider { get; set; }
            public TMP_InputField NameInput { get; set; }
            public TMP_Dropdown ModeDropdown { get; set; }
            public UIListView List { get; set; }

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
