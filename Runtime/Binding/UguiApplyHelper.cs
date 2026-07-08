using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Components;

namespace KK.UI.UMG.Binding
{
    public static class UguiApplyHelper
    {
        public static void ApplyOrThrow(object control, string property, object value)
        {
            if (TryApply(control, property, value))
            {
                return;
            }

            throw new NotSupportedException($"Cannot apply binding property '{property}' to control '{control?.GetType().Name}'.");
        }

        public static bool TryApply(Transform target, string property, object value)
        {
            if (target == null)
            {
                return false;
            }

            if (target.TryGetComponent<TMP_Text>(out var text) && TryApply(text, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<Image>(out var image) && TryApply(image, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<RawImage>(out var rawImage) && TryApply(rawImage, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<Button>(out var button) && TryApply(button, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<Toggle>(out var toggle) && TryApply(toggle, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<Slider>(out var slider) && TryApply(slider, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<Scrollbar>(out var scrollbar) && TryApply(scrollbar, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<TMP_InputField>(out var inputField) && TryApply(inputField, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<TMP_Dropdown>(out var dropdown) && TryApply(dropdown, property, value))
            {
                return true;
            }

            if (target.TryGetComponent<UIListView>(out var listView) && TryApply(listView, property, value))
            {
                return true;
            }

            return false;
        }

        public static bool TryApply(object control, string property, object value)
        {
            if (control is TMP_Text text && Is(property, "text"))
            {
                text.text = value?.ToString() ?? string.Empty;
                return true;
            }

            if (control is Image image && Is(property, "sprite"))
            {
                image.sprite = value as Sprite;
                return true;
            }

            if (control is RawImage rawImage && Is(property, "texture"))
            {
                rawImage.texture = value as Texture;
                return true;
            }

            if (control is Button button && Is(property, "interactable"))
            {
                button.interactable = value is bool enabled && enabled;
                return true;
            }

            if (control is Toggle toggle)
            {
                if (Is(property, "isOn"))
                {
                    toggle.isOn = value is bool isOn && isOn;
                    return true;
                }

                if (Is(property, "interactable"))
                {
                    toggle.interactable = value is bool enabled && enabled;
                    return true;
                }
            }

            if (control is Slider slider)
            {
                if (Is(property, "value"))
                {
                    slider.value = ToFloat(value);
                    return true;
                }

                if (Is(property, "interactable"))
                {
                    slider.interactable = value is bool enabled && enabled;
                    return true;
                }
            }

            if (control is Scrollbar scrollbar && Is(property, "value"))
            {
                scrollbar.value = ToFloat(value);
                return true;
            }

            if (control is TMP_InputField inputField)
            {
                if (Is(property, "text"))
                {
                    inputField.text = value?.ToString() ?? string.Empty;
                    return true;
                }

                if (Is(property, "interactable"))
                {
                    inputField.interactable = value is bool enabled && enabled;
                    return true;
                }
            }

            if (control is TMP_Dropdown dropdown)
            {
                if (Is(property, "value"))
                {
                    dropdown.value = value is int intValue ? intValue : Mathf.RoundToInt(ToFloat(value));
                    return true;
                }

                if (Is(property, "interactable"))
                {
                    dropdown.interactable = value is bool enabled && enabled;
                    return true;
                }
            }

            if (control is UIListView listView && Is(property, "items"))
            {
                listView.SetItems(value as IReadOnlyList<MessagePayload>);
                return true;
            }

            return false;
        }

        private static bool Is(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
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
    }
}
