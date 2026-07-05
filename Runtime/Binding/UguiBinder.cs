using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Components;

namespace KK.UI.UMG.Binding
{
    public sealed class UguiBinder
    {
        private readonly Dictionary<string, ViewModelBinding> _bindingsByField = new Dictionary<string, ViewModelBinding>();
        private readonly Dictionary<string, object> _controlsById = new Dictionary<string, object>();

        public void Configure(UIViewBase view, IReadOnlyList<ViewModelBinding> bindings)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            _bindingsByField.Clear();
            _controlsById.Clear();

            var viewType = view.GetType();
            foreach (var binding in bindings)
            {
                if (binding.Mode != BindingMode.OneWay)
                {
                    throw new NotSupportedException($"Binding mode '{binding.Mode}' is not supported in v1.0.");
                }

                var property = viewType.GetProperty(binding.ControlId) ?? viewType.GetProperty(ToPascal(binding.ControlId));
                if (property == null)
                {
                    throw new InvalidOperationException($"View '{viewType.Name}' does not expose control '{binding.ControlId}'.");
                }

                _bindingsByField[binding.FieldId] = binding;
                _controlsById[binding.ControlId] = property.GetValue(view);
            }
        }

        public void Flush(ViewModelStore store)
        {
            foreach (var dirty in store.TakeDirty())
            {
                if (!_bindingsByField.TryGetValue(dirty.FieldId, out var binding))
                {
                    continue;
                }

                if (!_controlsById.TryGetValue(binding.ControlId, out var control))
                {
                    continue;
                }

                Apply(control, binding.Property, dirty.Value);
            }
        }

        private static void Apply(object control, string property, object value)
        {
            if (control is TMP_Text text && string.Equals(property, "text", StringComparison.OrdinalIgnoreCase))
            {
                text.text = value?.ToString() ?? string.Empty;
                return;
            }

            if (control is Image image && string.Equals(property, "sprite", StringComparison.OrdinalIgnoreCase))
            {
                image.sprite = value as Sprite;
                return;
            }

            if (control is RawImage rawImage && string.Equals(property, "texture", StringComparison.OrdinalIgnoreCase))
            {
                rawImage.texture = value as Texture;
                return;
            }

            if (control is Button button && string.Equals(property, "interactable", StringComparison.OrdinalIgnoreCase))
            {
                button.interactable = value is bool enabled && enabled;
                return;
            }

            if (control is Toggle toggle)
            {
                if (string.Equals(property, "isOn", StringComparison.OrdinalIgnoreCase))
                {
                    toggle.isOn = value is bool isOn && isOn;
                    return;
                }

                if (string.Equals(property, "interactable", StringComparison.OrdinalIgnoreCase))
                {
                    toggle.interactable = value is bool enabled && enabled;
                    return;
                }
            }

            if (control is Slider slider)
            {
                if (string.Equals(property, "value", StringComparison.OrdinalIgnoreCase))
                {
                    slider.value = ToFloat(value);
                    return;
                }

                if (string.Equals(property, "interactable", StringComparison.OrdinalIgnoreCase))
                {
                    slider.interactable = value is bool enabled && enabled;
                    return;
                }
            }

            if (control is Scrollbar scrollbar && string.Equals(property, "value", StringComparison.OrdinalIgnoreCase))
            {
                scrollbar.value = ToFloat(value);
                return;
            }

            if (control is TMP_InputField inputField)
            {
                if (string.Equals(property, "text", StringComparison.OrdinalIgnoreCase))
                {
                    inputField.text = value?.ToString() ?? string.Empty;
                    return;
                }

                if (string.Equals(property, "interactable", StringComparison.OrdinalIgnoreCase))
                {
                    inputField.interactable = value is bool enabled && enabled;
                    return;
                }
            }

            if (control is TMP_Dropdown dropdown)
            {
                if (string.Equals(property, "value", StringComparison.OrdinalIgnoreCase))
                {
                    dropdown.value = value is int intValue ? intValue : Mathf.RoundToInt(ToFloat(value));
                    return;
                }

                if (string.Equals(property, "interactable", StringComparison.OrdinalIgnoreCase))
                {
                    dropdown.interactable = value is bool enabled && enabled;
                    return;
                }
            }

            if (control is UIListView listView && string.Equals(property, "items", StringComparison.OrdinalIgnoreCase))
            {
                listView.SetItems(value as IReadOnlyList<MessagePayload>);
                return;
            }

            throw new NotSupportedException($"Cannot apply binding property '{property}' to control '{control?.GetType().Name}'.");
        }

        private static float ToFloat(object value)
        {
            switch (value)
            {
                case float floatValue:
                    return floatValue;
                case int intValue:
                    return intValue;
                case double doubleValue:
                    return (float)doubleValue;
                default:
                    return 0f;
            }
        }

        private static string ToPascal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
