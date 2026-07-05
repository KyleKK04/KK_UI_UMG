using System.Collections.Generic;
using System.Text.RegularExpressions;
using KK.UI.UMG.Editor.Manifests;

namespace KK.UI.UMG.Editor.Validators
{
    internal static class ValidatorUtility
    {
        public static readonly HashSet<string> SupportedControls = new HashSet<string>
        {
            "Panel",
            "Text",
            "Image",
            "Button",
            "RawImage",
            "Toggle",
            "Slider",
            "InputField",
            "Dropdown",
            "Scrollbar",
            "ScrollView",
            "VerticalList"
        };

        public static bool IsCSharpIdentifier(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, @"^[_A-Za-z][_A-Za-z0-9]*$");
        }

        public static bool IsNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var part in value.Split('.'))
            {
                if (!IsCSharpIdentifier(part))
                {
                    return false;
                }
            }

            return true;
        }

        public static void Walk(UiLayoutNode node, System.Action<UiLayoutNode> visitor)
        {
            if (node == null)
            {
                return;
            }

            visitor(node);
            if (node.Children == null)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                Walk(child, visitor);
            }
        }
    }
}
