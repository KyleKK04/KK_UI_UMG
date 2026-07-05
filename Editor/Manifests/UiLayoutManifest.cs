using System.Collections.Generic;
using Newtonsoft.Json;

namespace KK.UI.UMG.Editor.Manifests
{
    public sealed class UiLayoutManifest
    {
        [JsonProperty("root")] public UiLayoutNode Root { get; set; }
    }

    public sealed class UiLayoutNode
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("rect")] public UiRectSpec Rect { get; set; }
        [JsonProperty("text")] public UiTextSpec Text { get; set; }
        [JsonProperty("image")] public UiImageSpec Image { get; set; }
        [JsonProperty("rawImage")] public UiRawImageSpec RawImage { get; set; }
        [JsonProperty("button")] public UiButtonSpec Button { get; set; }
        [JsonProperty("toggle")] public UiToggleSpec Toggle { get; set; }
        [JsonProperty("slider")] public UiSliderSpec Slider { get; set; }
        [JsonProperty("inputField")] public UiInputFieldSpec InputField { get; set; }
        [JsonProperty("dropdown")] public UiDropdownSpec Dropdown { get; set; }
        [JsonProperty("scrollbar")] public UiScrollbarSpec Scrollbar { get; set; }
        [JsonProperty("scrollView")] public UiScrollViewSpec ScrollView { get; set; }
        [JsonProperty("verticalList")] public UiVerticalListSpec VerticalList { get; set; }
        [JsonProperty("layoutComponents")] public UiLayoutComponentsSpec LayoutComponents { get; set; }
        [JsonProperty("children")] public List<UiLayoutNode> Children { get; set; } = new List<UiLayoutNode>();
    }

    public sealed class UiRectSpec
    {
        [JsonProperty("anchorMin")] public float[] AnchorMin { get; set; }
        [JsonProperty("anchorMax")] public float[] AnchorMax { get; set; }
        [JsonProperty("position")] public float[] Position { get; set; }
        [JsonProperty("size")] public float[] Size { get; set; }
        [JsonProperty("sizeDelta")] public float[] SizeDelta { get; set; }
    }

    public sealed class UiTextSpec
    {
        [JsonProperty("value")] public string Value { get; set; }
        [JsonProperty("locKey")] public string LocKey { get; set; }
        [JsonProperty("fontSize")] public float FontSize { get; set; } = 24f;
        [JsonProperty("fontAsset")] public string FontAsset { get; set; }
        [JsonProperty("color")] public string Color { get; set; }
        [JsonProperty("alignment")] public string Alignment { get; set; }
    }

    public sealed class UiImageSpec
    {
        [JsonProperty("color")] public string Color { get; set; }
        [JsonProperty("sprite")] public string Sprite { get; set; }
    }

    public sealed class UiRawImageSpec
    {
        [JsonProperty("texture")] public string Texture { get; set; }
        [JsonProperty("color")] public string Color { get; set; }
    }

    public sealed class UiButtonSpec
    {
        [JsonProperty("interactable")] public bool Interactable { get; set; } = true;
    }

    public sealed class UiToggleSpec
    {
        [JsonProperty("isOn")] public bool IsOn { get; set; }
        [JsonProperty("labelLocKey")] public string LabelLocKey { get; set; }
    }

    public sealed class UiSliderSpec
    {
        [JsonProperty("minValue")] public float MinValue { get; set; }
        [JsonProperty("maxValue")] public float MaxValue { get; set; } = 1f;
        [JsonProperty("wholeNumbers")] public bool WholeNumbers { get; set; }
    }

    public sealed class UiInputFieldSpec
    {
        [JsonProperty("textLocKey")] public string TextLocKey { get; set; }
        [JsonProperty("placeholderLocKey")] public string PlaceholderLocKey { get; set; }
        [JsonProperty("characterLimit")] public int CharacterLimit { get; set; }
    }

    public sealed class UiDropdownSpec
    {
        [JsonProperty("options")] public List<UiDropdownOptionSpec> Options { get; set; } = new List<UiDropdownOptionSpec>();
    }

    public sealed class UiDropdownOptionSpec
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("locKey")] public string LocKey { get; set; }
    }

    public sealed class UiScrollbarSpec
    {
        [JsonProperty("direction")] public string Direction { get; set; }
        [JsonProperty("value")] public float Value { get; set; }
        [JsonProperty("size")] public float Size { get; set; } = 0.2f;
    }

    public sealed class UiScrollViewSpec
    {
        [JsonProperty("vertical")] public bool Vertical { get; set; } = true;
        [JsonProperty("horizontal")] public bool Horizontal { get; set; }
    }

    public sealed class UiVerticalListSpec
    {
        [JsonProperty("itemSourceField")] public string ItemSourceField { get; set; }
        [JsonProperty("itemTemplate")] public UiLayoutNode ItemTemplate { get; set; }
        [JsonProperty("itemBindings")] public List<UiListItemBindingSpec> ItemBindings { get; set; } = new List<UiListItemBindingSpec>();
        [JsonProperty("itemEvents")] public List<UiListItemEventSpec> ItemEvents { get; set; } = new List<UiListItemEventSpec>();
    }

    public sealed class UiListItemBindingSpec
    {
        [JsonProperty("controlId")] public string ControlId { get; set; }
        [JsonProperty("itemField")] public string ItemField { get; set; }
        [JsonProperty("property")] public string Property { get; set; }
    }

    public sealed class UiListItemEventSpec
    {
        [JsonProperty("controlId")] public string ControlId { get; set; }
        [JsonProperty("event")] public string Event { get; set; }
        [JsonProperty("handler")] public string Handler { get; set; }
        [JsonProperty("itemIdField")] public string ItemIdField { get; set; }
    }

    public sealed class UiLayoutComponentsSpec
    {
        [JsonProperty("layoutElement")] public UiLayoutElementSpec LayoutElement { get; set; }
        [JsonProperty("horizontalLayout")] public UiHorizontalLayoutSpec HorizontalLayout { get; set; }
        [JsonProperty("verticalLayout")] public UiVerticalLayoutSpec VerticalLayout { get; set; }
        [JsonProperty("gridLayout")] public UiGridLayoutSpec GridLayout { get; set; }
        [JsonProperty("contentSizeFitter")] public UiContentSizeFitterSpec ContentSizeFitter { get; set; }
        [JsonProperty("aspectRatioFitter")] public UiAspectRatioFitterSpec AspectRatioFitter { get; set; }
    }

    public sealed class UiLayoutElementSpec
    {
        [JsonProperty("ignoreLayout")] public bool IgnoreLayout { get; set; }
        [JsonProperty("minWidth")] public float MinWidth { get; set; } = -1f;
        [JsonProperty("minHeight")] public float MinHeight { get; set; } = -1f;
        [JsonProperty("preferredWidth")] public float PreferredWidth { get; set; } = -1f;
        [JsonProperty("preferredHeight")] public float PreferredHeight { get; set; } = -1f;
        [JsonProperty("flexibleWidth")] public float FlexibleWidth { get; set; } = -1f;
        [JsonProperty("flexibleHeight")] public float FlexibleHeight { get; set; } = -1f;
        [JsonProperty("layoutPriority")] public int LayoutPriority { get; set; } = 1;
    }

    public abstract class UiLayoutGroupSpec
    {
        [JsonProperty("padding")] public UiPaddingSpec Padding { get; set; }
        [JsonProperty("spacing")] public float Spacing { get; set; }
        [JsonProperty("childAlignment")] public string ChildAlignment { get; set; } = "UpperLeft";
        [JsonProperty("childControlWidth")] public bool ChildControlWidth { get; set; } = true;
        [JsonProperty("childControlHeight")] public bool ChildControlHeight { get; set; } = true;
        [JsonProperty("childForceExpandWidth")] public bool ChildForceExpandWidth { get; set; }
        [JsonProperty("childForceExpandHeight")] public bool ChildForceExpandHeight { get; set; }
        [JsonProperty("childScaleWidth")] public bool ChildScaleWidth { get; set; }
        [JsonProperty("childScaleHeight")] public bool ChildScaleHeight { get; set; }
        [JsonProperty("reverseArrangement")] public bool ReverseArrangement { get; set; }
    }

    public sealed class UiHorizontalLayoutSpec : UiLayoutGroupSpec
    {
    }

    public sealed class UiVerticalLayoutSpec : UiLayoutGroupSpec
    {
    }

    public sealed class UiGridLayoutSpec
    {
        [JsonProperty("padding")] public UiPaddingSpec Padding { get; set; }
        [JsonProperty("childAlignment")] public string ChildAlignment { get; set; } = "UpperLeft";
        [JsonProperty("cellSize")] public UiVector2Spec CellSize { get; set; }
        [JsonProperty("spacing")] public UiVector2Spec Spacing { get; set; }
        [JsonProperty("startCorner")] public string StartCorner { get; set; } = "UpperLeft";
        [JsonProperty("startAxis")] public string StartAxis { get; set; } = "Horizontal";
        [JsonProperty("constraint")] public string Constraint { get; set; } = "Flexible";
        [JsonProperty("constraintCount")] public int ConstraintCount { get; set; }
    }

    public sealed class UiContentSizeFitterSpec
    {
        [JsonProperty("horizontalFit")] public string HorizontalFit { get; set; } = "Unconstrained";
        [JsonProperty("verticalFit")] public string VerticalFit { get; set; } = "Unconstrained";
    }

    public sealed class UiAspectRatioFitterSpec
    {
        [JsonProperty("aspectMode")] public string AspectMode { get; set; } = "FitInParent";
        [JsonProperty("aspectRatio")] public float AspectRatio { get; set; } = 1f;
    }

    public sealed class UiPaddingSpec
    {
        [JsonProperty("left")] public int Left { get; set; }
        [JsonProperty("right")] public int Right { get; set; }
        [JsonProperty("top")] public int Top { get; set; }
        [JsonProperty("bottom")] public int Bottom { get; set; }
    }

    public sealed class UiVector2Spec
    {
        [JsonProperty("x")] public float X { get; set; }
        [JsonProperty("y")] public float Y { get; set; }
    }
}
