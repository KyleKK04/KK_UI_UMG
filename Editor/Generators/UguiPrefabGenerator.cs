using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Components;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Internal;

namespace KK.UI.UMG.Editor.Generators
{
    public sealed class UguiPrefabGenerator
    {
        public string Generate(KKUIPipelineContext context)
        {
            var prefabRoot = Path.Combine(context.GeneratedRoot, "Prefabs");
            Directory.CreateDirectory(prefabRoot);
            var prefabPath = ToAssetPath(Path.Combine(prefabRoot, $"{context.Package.PackageId}View.prefab"));
            EnsureCanOverwrite(prefabPath);

            var assetIndex = BuildAssetIndex(context);
            var root = CreateNode(context, context.Layout.Root, null, new Dictionary<string, Component>(), assetIndex);
            root.name = context.Codegen.View.ClassName;
            EnsureLayerComponents(root);
            var marker = root.AddComponent<GeneratedAssetMarker>();
            marker.Initialize(context.Package.PackageId);

            var viewType = FindType(context.Codegen.Namespace, context.Codegen.View.ClassName);
            if (viewType == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException($"Generated view type '{context.Codegen.View.ClassName}' is not compiled yet.");
            }

            EnsureCompiledViewMatchesManifest(context, viewType, root);
            var view = root.AddComponent(viewType);
            FillSerializedFields(view, root);
            EnsureTextFonts(root);

            root.SetActive(false);
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            RegisterAddressable(prefabPath, context.Codegen.AddressablesKey, context.Package.AddressablesGroup);
            UnityEngine.Object.DestroyImmediate(root);
            return prefabPath;
        }

        private static GameObject CreateNode(KKUIPipelineContext context, UiLayoutNode node, Transform parent, Dictionary<string, Component> controls, Dictionary<string, UiAssetSpec> assetIndex)
        {
            var gameObject = new GameObject(node.Id, typeof(RectTransform));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            var rect = gameObject.GetComponent<RectTransform>();
            ApplyRect(rect, node.Rect, parent == null);

            var childParent = gameObject.transform;
            switch (node.Type)
            {
                case "Panel":
                {
                    if (node.Image != null)
                    {
                        var image = gameObject.AddComponent<Image>();
                        image.color = ParseColor(node.Image.Color, new Color(0f, 0f, 0f, 0.35f));
                        image.sprite = LoadSprite(context, assetIndex, node.Image.Sprite);
                    }
                    break;
                }
                case "Text":
                {
                    var text = gameObject.AddComponent<TextMeshProUGUI>();
                    text.text = ResolvePreviewText(context, node.Text);
                    text.fontSize = node.Text?.FontSize > 0 ? node.Text.FontSize : 24f;
                    text.font = LoadFontAsset(context, assetIndex, node.Text?.FontAsset) ?? LoadDefaultFontAsset();
                    text.color = ParseColor(node.Text?.Color, Color.white);
                    controls[node.Id] = text;
                    break;
                }
                case "Image":
                {
                    var image = gameObject.AddComponent<Image>();
                    image.color = ParseColor(node.Image?.Color, Color.white);
                    image.sprite = LoadSprite(context, assetIndex, node.Image?.Sprite);
                    controls[node.Id] = image;
                    break;
                }
                case "Button":
                {
                    var buttonImage = gameObject.AddComponent<Image>();
                    buttonImage.color = ParseColor(node.Image?.Color, new Color(0.2f, 0.45f, 0.9f, 1f));
                    buttonImage.sprite = LoadSprite(context, assetIndex, node.Image?.Sprite);
                    var button = gameObject.AddComponent<Button>();
                    button.interactable = node.Button?.Interactable ?? true;
                    controls[node.Id] = button;
                    break;
                }
                case "RawImage":
                {
                    var rawImage = gameObject.AddComponent<RawImage>();
                    rawImage.color = ParseColor(node.RawImage?.Color, Color.white);
                    rawImage.texture = LoadTexture(context, assetIndex, node.RawImage?.Texture);
                    controls[node.Id] = rawImage;
                    break;
                }
                case "Toggle":
                {
                    var toggle = gameObject.AddComponent<Toggle>();
                    var background = CreateChild(gameObject.transform, "Background");
                    var backgroundImage = background.gameObject.AddComponent<Image>();
                    backgroundImage.color = new Color(1f, 1f, 1f, 0.28f);
                    var checkmark = CreateChild(background, "Checkmark");
                    var checkmarkImage = checkmark.gameObject.AddComponent<Image>();
                    checkmarkImage.color = new Color(0.2f, 0.55f, 1f, 1f);
                    ApplyRect(background.GetComponent<RectTransform>(), FixedRect(0f, 0.5f, 0f, 0.5f, 20f, 0f, 32f, 32f), false);
                    ApplyRect(checkmark.GetComponent<RectTransform>(), FixedRect(0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 20f, 20f), false);
                    var label = CreateTextObject(context, gameObject.transform, "Label", ResolveLocKey(context, node.Toggle?.LabelLocKey), 24f, Color.white);
                    ApplyRect(label.GetComponent<RectTransform>(), FixedRect(0f, 0f, 1f, 1f, 56f, 0f, -56f, 0f), false);
                    toggle.targetGraphic = backgroundImage;
                    toggle.graphic = checkmarkImage;
                    toggle.isOn = node.Toggle?.IsOn ?? false;
                    controls[node.Id] = toggle;
                    break;
                }
                case "Slider":
                {
                    var slider = gameObject.AddComponent<Slider>();
                    slider.minValue = node.Slider?.MinValue ?? 0f;
                    slider.maxValue = node.Slider?.MaxValue ?? 1f;
                    slider.wholeNumbers = node.Slider?.WholeNumbers ?? false;
                    BuildSliderVisuals(gameObject.transform, slider);
                    controls[node.Id] = slider;
                    break;
                }
                case "InputField":
                {
                    var image = gameObject.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.18f);
                    var input = gameObject.AddComponent<TMP_InputField>();
                    input.targetGraphic = image;
                    input.characterLimit = node.InputField?.CharacterLimit ?? 0;
                    BuildInputFieldVisuals(context, gameObject.transform, input, node.InputField);
                    controls[node.Id] = input;
                    break;
                }
                case "Dropdown":
                {
                    var image = gameObject.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.18f);
                    var dropdown = gameObject.AddComponent<TMP_Dropdown>();
                    dropdown.targetGraphic = image;
                    BuildDropdownVisuals(context, gameObject.transform, dropdown, node.Dropdown);
                    controls[node.Id] = dropdown;
                    break;
                }
                case "Scrollbar":
                {
                    var image = gameObject.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.12f);
                    var scrollbar = gameObject.AddComponent<Scrollbar>();
                    scrollbar.targetGraphic = image;
                    scrollbar.direction = ParseScrollbarDirection(node.Scrollbar?.Direction);
                    scrollbar.value = node.Scrollbar?.Value ?? 0f;
                    scrollbar.size = node.Scrollbar?.Size ?? 0.2f;
                    BuildScrollbarVisuals(gameObject.transform, scrollbar);
                    controls[node.Id] = scrollbar;
                    break;
                }
                case "ScrollView":
                {
                    var scrollRect = gameObject.AddComponent<ScrollRect>();
                    var content = BuildScrollContent(gameObject.transform, scrollRect, node.ScrollView?.Vertical ?? true, node.ScrollView?.Horizontal ?? false);
                    childParent = content;
                    controls[node.Id] = scrollRect;
                    break;
                }
                case "VerticalList":
                {
                    var scrollRect = gameObject.AddComponent<ScrollRect>();
                    var content = BuildScrollContent(gameObject.transform, scrollRect, true, false);
                    var template = node.VerticalList?.ItemTemplate != null
                        ? CreateNode(context, node.VerticalList.ItemTemplate, content, controls, assetIndex)
                        : CreateDefaultListItemTemplate(context, content);
                    template.name = "ItemTemplate";
                    EnsureListItemLayout(template);
                    template.SetActive(false);
                    var listView = gameObject.AddComponent<UIListView>();
                    listView.Configure(content.GetComponent<RectTransform>(), template, BuildItemBindings(node.VerticalList), BuildItemEvents(node.VerticalList));
                    controls[node.Id] = listView;
                    break;
                }
                default:
                    throw new NotSupportedException($"Unsupported layout node type '{node.Type}'.");
            }

            ApplyLayoutComponents(gameObject, node.LayoutComponents);

            foreach (var child in node.Children ?? new List<UiLayoutNode>())
            {
                CreateNode(context, child, childParent, controls, assetIndex);
            }

            return gameObject;
        }

        private static RectTransform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static UiRectSpec FixedRect(float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY, float positionX, float positionY, float sizeX, float sizeY)
        {
            return new UiRectSpec
            {
                AnchorMin = new[] { anchorMinX, anchorMinY },
                AnchorMax = new[] { anchorMaxX, anchorMaxY },
                Position = new[] { positionX, positionY },
                Size = new[] { sizeX, sizeY }
            };
        }

        private static TextMeshProUGUI CreateTextObject(KKUIPipelineContext context, Transform parent, string name, string value, float fontSize, Color color)
        {
            var rect = CreateChild(parent, name);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.fontSize = fontSize;
            text.font = LoadDefaultFontAsset();
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            return text;
        }

        private static void BuildSliderVisuals(Transform root, Slider slider)
        {
            var background = CreateChild(root, "Background");
            background.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);
            ApplyRect(background, FixedRect(0f, 0.5f, 1f, 0.5f, 0f, 0f, 0f, 10f), false);

            var fillArea = CreateChild(root, "Fill Area");
            ApplyRect(fillArea, FixedRect(0f, 0f, 1f, 1f, 8f, 0f, -16f, 0f), false);
            var fill = CreateChild(fillArea, "Fill");
            fill.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.55f, 1f, 1f);
            ApplyRect(fill, FixedRect(0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f), false);

            var handleArea = CreateChild(root, "Handle Slide Area");
            ApplyRect(handleArea, FixedRect(0f, 0f, 1f, 1f, 10f, 0f, -20f, 0f), false);
            var handle = CreateChild(handleArea, "Handle");
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;
            ApplyRect(handle, FixedRect(0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 24f, 24f), false);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
        }

        private static void BuildInputFieldVisuals(KKUIPipelineContext context, Transform root, TMP_InputField input, UiInputFieldSpec spec)
        {
            var textViewport = CreateChild(root, "Text Area");
            ApplyRect(textViewport, FixedRect(0f, 0f, 1f, 1f, 12f, 0f, -24f, 0f), false);
            var placeholder = CreateTextObject(context, textViewport, "Placeholder", ResolveLocKey(context, spec?.PlaceholderLocKey), 24f, new Color(1f, 1f, 1f, 0.42f));
            ApplyRect(placeholder.GetComponent<RectTransform>(), FixedRect(0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f), false);
            var text = CreateTextObject(context, textViewport, "Text", ResolveLocKey(context, spec?.TextLocKey), 24f, Color.white);
            ApplyRect(text.GetComponent<RectTransform>(), FixedRect(0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f), false);

            input.textViewport = textViewport;
            input.textComponent = text;
            input.placeholder = placeholder;
        }

        private static void BuildDropdownVisuals(KKUIPipelineContext context, Transform root, TMP_Dropdown dropdown, UiDropdownSpec spec)
        {
            var label = CreateTextObject(context, root, "Label", string.Empty, 24f, Color.white);
            ApplyRect(label.GetComponent<RectTransform>(), FixedRect(0f, 0f, 1f, 1f, 12f, 0f, -48f, 0f), false);
            dropdown.captionText = label;
            dropdown.options.Clear();
            foreach (var option in spec?.Options ?? new List<UiDropdownOptionSpec>())
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(ResolveLocKey(context, option.LocKey)));
            }

            if (dropdown.options.Count == 0)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(string.Empty));
            }

            var template = CreateChild(root, "Template");
            template.gameObject.SetActive(false);
            var templateImage = template.gameObject.AddComponent<Image>();
            templateImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            var scrollRect = template.gameObject.AddComponent<ScrollRect>();
            ApplyRect(template, FixedRect(0f, 0f, 1f, 0f, 0f, -80f, 0f, 160f), false);
            var content = BuildScrollContent(template, scrollRect, true, false);
            var item = CreateChild(content, "Item");
            var toggle = item.gameObject.AddComponent<Toggle>();
            var itemBackground = item.gameObject.AddComponent<Image>();
            itemBackground.color = new Color(1f, 1f, 1f, 0.08f);
            toggle.targetGraphic = itemBackground;
            ApplyRect(item, FixedRect(0f, 1f, 1f, 1f, 0f, -20f, 0f, 40f), false);
            var itemLabel = CreateTextObject(context, item, "Item Label", string.Empty, 22f, Color.white);
            ApplyRect(itemLabel.GetComponent<RectTransform>(), FixedRect(0f, 0f, 1f, 1f, 12f, 0f, -24f, 0f), false);

            dropdown.template = template;
            dropdown.itemText = itemLabel;
        }

        private static void BuildScrollbarVisuals(Transform root, Scrollbar scrollbar)
        {
            var slidingArea = CreateChild(root, "Sliding Area");
            ApplyRect(slidingArea, FixedRect(0f, 0f, 1f, 1f, 8f, 8f, -16f, -16f), false);
            var handle = CreateChild(slidingArea, "Handle");
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;
            ApplyRect(handle, FixedRect(0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f), false);
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
        }

        private static RectTransform BuildScrollContent(Transform root, ScrollRect scrollRect, bool vertical, bool horizontal)
        {
            var viewport = CreateChild(root, "Viewport");
            viewport.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            ApplyRect(viewport, FixedRect(0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f), false);

            var content = CreateChild(viewport, "Content");
            ApplyRect(content, FixedRect(0f, 1f, 1f, 1f, 0f, 0f, 0f, 0f), false);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.vertical = vertical;
            scrollRect.horizontal = horizontal;
            return content;
        }

        private static GameObject CreateDefaultListItemTemplate(KKUIPipelineContext context, Transform content)
        {
            var item = new GameObject("ItemTemplate", typeof(RectTransform));
            item.transform.SetParent(content, false);
            ApplyRect(item.GetComponent<RectTransform>(), FixedRect(0f, 1f, 1f, 1f, 0f, 0f, 0f, 48f), false);
            item.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
            var label = CreateTextObject(context, item.transform, "Label", string.Empty, 22f, Color.white);
            ApplyRect(label.GetComponent<RectTransform>(), FixedRect(0f, 0f, 1f, 1f, 12f, 0f, -24f, 0f), false);
            return item;
        }

        private static void EnsureListItemLayout(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            var rect = item.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            var layout = item.GetComponent<LayoutElement>() ?? item.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.preferredHeight = rect.sizeDelta.y > 0f ? rect.sizeDelta.y : 48f;
        }

        private static void ApplyLayoutComponents(GameObject gameObject, UiLayoutComponentsSpec spec)
        {
            if (spec == null)
            {
                return;
            }

            if (spec.LayoutElement != null)
            {
                ApplyLayoutElement(gameObject, spec.LayoutElement);
            }

            if (spec.HorizontalLayout != null)
            {
                ApplyHorizontalLayout(gameObject, spec.HorizontalLayout);
            }

            if (spec.VerticalLayout != null)
            {
                ApplyVerticalLayout(gameObject, spec.VerticalLayout);
            }

            if (spec.GridLayout != null)
            {
                ApplyGridLayout(gameObject, spec.GridLayout);
            }

            if (spec.ContentSizeFitter != null)
            {
                ApplyContentSizeFitter(gameObject, spec.ContentSizeFitter);
            }

            if (spec.AspectRatioFitter != null)
            {
                ApplyAspectRatioFitter(gameObject, spec.AspectRatioFitter);
            }
        }

        private static void ApplyLayoutElement(GameObject gameObject, UiLayoutElementSpec spec)
        {
            var layout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = spec.IgnoreLayout;
            layout.minWidth = spec.MinWidth;
            layout.minHeight = spec.MinHeight;
            layout.preferredWidth = spec.PreferredWidth;
            layout.preferredHeight = spec.PreferredHeight;
            layout.flexibleWidth = spec.FlexibleWidth;
            layout.flexibleHeight = spec.FlexibleHeight;
            layout.layoutPriority = spec.LayoutPriority;
        }

        private static void ApplyHorizontalLayout(GameObject gameObject, UiHorizontalLayoutSpec spec)
        {
            var layout = gameObject.GetComponent<HorizontalLayoutGroup>() ?? gameObject.AddComponent<HorizontalLayoutGroup>();
            ApplyLayoutGroup(layout, spec);
        }

        private static void ApplyVerticalLayout(GameObject gameObject, UiVerticalLayoutSpec spec)
        {
            var layout = gameObject.GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
            ApplyLayoutGroup(layout, spec);
        }

        private static void ApplyLayoutGroup(HorizontalOrVerticalLayoutGroup layout, UiLayoutGroupSpec spec)
        {
            layout.padding = ToRectOffset(spec.Padding);
            layout.spacing = spec.Spacing;
            layout.childAlignment = ParseTextAnchor(spec.ChildAlignment);
            layout.childControlWidth = spec.ChildControlWidth;
            layout.childControlHeight = spec.ChildControlHeight;
            layout.childForceExpandWidth = spec.ChildForceExpandWidth;
            layout.childForceExpandHeight = spec.ChildForceExpandHeight;
            layout.childScaleWidth = spec.ChildScaleWidth;
            layout.childScaleHeight = spec.ChildScaleHeight;
            layout.reverseArrangement = spec.ReverseArrangement;
        }

        private static void ApplyGridLayout(GameObject gameObject, UiGridLayoutSpec spec)
        {
            var layout = gameObject.GetComponent<GridLayoutGroup>() ?? gameObject.AddComponent<GridLayoutGroup>();
            layout.padding = ToRectOffset(spec.Padding);
            layout.childAlignment = ParseTextAnchor(spec.ChildAlignment);
            layout.cellSize = ToVector2(spec.CellSize, new Vector2(100f, 100f));
            layout.spacing = ToVector2(spec.Spacing, Vector2.zero);
            layout.startCorner = ParseGridStartCorner(spec.StartCorner);
            layout.startAxis = ParseGridStartAxis(spec.StartAxis);
            layout.constraint = ParseGridConstraint(spec.Constraint);
            layout.constraintCount = spec.ConstraintCount;
        }

        private static void ApplyContentSizeFitter(GameObject gameObject, UiContentSizeFitterSpec spec)
        {
            var fitter = gameObject.GetComponent<ContentSizeFitter>() ?? gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ParseFitMode(spec.HorizontalFit);
            fitter.verticalFit = ParseFitMode(spec.VerticalFit);
        }

        private static void ApplyAspectRatioFitter(GameObject gameObject, UiAspectRatioFitterSpec spec)
        {
            var fitter = gameObject.GetComponent<AspectRatioFitter>() ?? gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = ParseAspectMode(spec.AspectMode);
            fitter.aspectRatio = spec.AspectRatio;
        }

        private static IReadOnlyList<UIListView.ItemBinding> BuildItemBindings(UiVerticalListSpec spec)
        {
            var result = new List<UIListView.ItemBinding>();
            foreach (var binding in spec?.ItemBindings ?? new List<UiListItemBindingSpec>())
            {
                result.Add(new UIListView.ItemBinding
                {
                    ControlId = binding.ControlId,
                    ItemField = binding.ItemField,
                    Property = binding.Property
                });
            }

            return result;
        }

        private static IReadOnlyList<UIListView.ItemEvent> BuildItemEvents(UiVerticalListSpec spec)
        {
            var result = new List<UIListView.ItemEvent>();
            foreach (var evt in spec?.ItemEvents ?? new List<UiListItemEventSpec>())
            {
                result.Add(new UIListView.ItemEvent
                {
                    ControlId = evt.ControlId,
                    Event = evt.Event,
                    Handler = evt.Handler,
                    ItemIdField = evt.ItemIdField
                });
            }

            return result;
        }

        private static Scrollbar.Direction ParseScrollbarDirection(string value)
        {
            return value switch
            {
                "BottomToTop" => Scrollbar.Direction.BottomToTop,
                "TopToBottom" => Scrollbar.Direction.TopToBottom,
                "RightToLeft" => Scrollbar.Direction.RightToLeft,
                _ => Scrollbar.Direction.LeftToRight
            };
        }

        private static RectOffset ToRectOffset(UiPaddingSpec padding)
        {
            return padding == null
                ? new RectOffset()
                : new RectOffset(padding.Left, padding.Right, padding.Top, padding.Bottom);
        }

        private static Vector2 ToVector2(UiVector2Spec spec, Vector2 fallback)
        {
            return spec == null ? fallback : new Vector2(spec.X, spec.Y);
        }

        private static TextAnchor ParseTextAnchor(string value)
        {
            return value switch
            {
                "UpperCenter" => TextAnchor.UpperCenter,
                "UpperRight" => TextAnchor.UpperRight,
                "MiddleLeft" => TextAnchor.MiddleLeft,
                "MiddleCenter" => TextAnchor.MiddleCenter,
                "MiddleRight" => TextAnchor.MiddleRight,
                "LowerLeft" => TextAnchor.LowerLeft,
                "LowerCenter" => TextAnchor.LowerCenter,
                "LowerRight" => TextAnchor.LowerRight,
                _ => TextAnchor.UpperLeft
            };
        }

        private static GridLayoutGroup.Corner ParseGridStartCorner(string value)
        {
            return value switch
            {
                "UpperRight" => GridLayoutGroup.Corner.UpperRight,
                "LowerLeft" => GridLayoutGroup.Corner.LowerLeft,
                "LowerRight" => GridLayoutGroup.Corner.LowerRight,
                _ => GridLayoutGroup.Corner.UpperLeft
            };
        }

        private static GridLayoutGroup.Axis ParseGridStartAxis(string value)
        {
            return value == "Vertical" ? GridLayoutGroup.Axis.Vertical : GridLayoutGroup.Axis.Horizontal;
        }

        private static GridLayoutGroup.Constraint ParseGridConstraint(string value)
        {
            return value switch
            {
                "FixedColumnCount" => GridLayoutGroup.Constraint.FixedColumnCount,
                "FixedRowCount" => GridLayoutGroup.Constraint.FixedRowCount,
                _ => GridLayoutGroup.Constraint.Flexible
            };
        }

        private static ContentSizeFitter.FitMode ParseFitMode(string value)
        {
            return value switch
            {
                "MinSize" => ContentSizeFitter.FitMode.MinSize,
                "PreferredSize" => ContentSizeFitter.FitMode.PreferredSize,
                _ => ContentSizeFitter.FitMode.Unconstrained
            };
        }

        private static AspectRatioFitter.AspectMode ParseAspectMode(string value)
        {
            return value switch
            {
                "None" => AspectRatioFitter.AspectMode.None,
                "WidthControlsHeight" => AspectRatioFitter.AspectMode.WidthControlsHeight,
                "HeightControlsWidth" => AspectRatioFitter.AspectMode.HeightControlsWidth,
                "EnvelopeParent" => AspectRatioFitter.AspectMode.EnvelopeParent,
                _ => AspectRatioFitter.AspectMode.FitInParent
            };
        }

        private static void EnsureLayerComponents(GameObject root)
        {
            if (root.GetComponent<CanvasGroup>() == null)
            {
                var canvasGroup = root.AddComponent<CanvasGroup>();
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }
        }

        private static void FillSerializedFields(Component view, GameObject root)
        {
            var fields = view.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var controlId = ToControlId(field.Name);
                var target = FindByName(root.transform, controlId) ?? FindByName(root.transform, char.ToLowerInvariant(controlId[0]) + controlId.Substring(1));
                if (target == null)
                {
                    continue;
                }

                var component = target.GetComponent(field.FieldType);
                if (component != null)
                {
                    field.SetValue(view, component);
                }
            }
        }

        private static void EnsureCompiledViewMatchesManifest(KKUIPipelineContext context, Type viewType, GameObject root)
        {
            ValidatorUtilityProxy.Walk(context.Layout.Root, node =>
            {
                if (!IsBindableControl(node.Type))
                {
                    return;
                }

                var fieldName = ToFieldName(node.Id);
                var field = viewType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                    throw new InvalidOperationException(
                        $"Compiled view type '{viewType.Name}' is out of date. Missing field '{fieldName}'. Wait for Unity to compile generated scripts, then run Generate again.");
                }
            });
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

        private static void ApplyRect(RectTransform rect, UiRectSpec spec, bool isRoot)
        {
            if (isRoot)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            if (spec?.AnchorMin != null && spec.AnchorMin.Length >= 2)
            {
                rect.anchorMin = new Vector2(spec.AnchorMin[0], spec.AnchorMin[1]);
            }

            if (spec?.AnchorMax != null && spec.AnchorMax.Length >= 2)
            {
                rect.anchorMax = new Vector2(spec.AnchorMax[0], spec.AnchorMax[1]);
            }

            if (spec?.Position != null && spec.Position.Length >= 2)
            {
                rect.anchoredPosition = new Vector2(spec.Position[0], spec.Position[1]);
            }

            var size = spec?.Size ?? spec?.SizeDelta;
            if (size != null && size.Length >= 2)
            {
                rect.sizeDelta = new Vector2(size[0], size[1]);
            }
        }

        private static Color ParseColor(string html, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return fallback;
            }

            return ColorUtility.TryParseHtmlString(html, out var color) ? color : fallback;
        }

        private static string ResolvePreviewText(KKUIPipelineContext context, UiTextSpec text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(text.LocKey))
            {
                return text.Value ?? string.Empty;
            }

            if (context.Strings.Strings.TryGetValue(text.LocKey, out var localizedValues) &&
                localizedValues.TryGetValue(context.Strings.DefaultCulture, out var value))
            {
                return value;
            }

            return text.LocKey;
        }

        private static Dictionary<string, UiAssetSpec> BuildAssetIndex(KKUIPipelineContext context)
        {
            var result = new Dictionary<string, UiAssetSpec>();
            foreach (var asset in context.Assets.Assets ?? new List<UiAssetSpec>())
            {
                if (!string.IsNullOrWhiteSpace(asset.Id))
                {
                    result[asset.Id] = asset;
                }
            }

            return result;
        }

        private static TMP_FontAsset LoadFontAsset(KKUIPipelineContext context, Dictionary<string, UiAssetSpec> assetIndex, string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return null;
            }

            var assetPath = ResolveRuntimeAssetPath(context, assetIndex, assetId, "TMP_FontAsset");
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset == null)
            {
                throw new InvalidOperationException($"Text fontAsset '{assetId}' resolved to '{assetPath}', but it does not exist or is not a TMP_FontAsset.");
            }

            return fontAsset;
        }

        private static Sprite LoadSprite(KKUIPipelineContext context, Dictionary<string, UiAssetSpec> assetIndex, string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return null;
            }

            var assetPath = ResolveRuntimeAssetPath(context, assetIndex, assetId, "Sprite");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                EnsureSpriteImporter(assetPath);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            if (sprite == null)
            {
                throw new InvalidOperationException($"Image sprite '{assetId}' resolved to '{assetPath}', but it does not exist or is not a Sprite.");
            }

            return sprite;
        }

        private static Texture LoadTexture(KKUIPipelineContext context, Dictionary<string, UiAssetSpec> assetIndex, string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return null;
            }

            var assetPath = ResolveRuntimeAssetPath(context, assetIndex, assetId, "Texture");
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException($"RawImage texture '{assetId}' resolved to '{assetPath}', but it does not exist or is not a Texture.");
            }

            return texture;
        }

        private static void EnsureSpriteImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static string ResolveRuntimeAssetPath(KKUIPipelineContext context, Dictionary<string, UiAssetSpec> assetIndex, string assetId, string expectedType)
        {
            if (!assetIndex.TryGetValue(assetId, out var asset))
            {
                throw new InvalidOperationException($"Asset id '{assetId}' is not declared.");
            }

            if (asset.Type != expectedType)
            {
                throw new InvalidOperationException($"Asset id '{assetId}' is type '{asset.Type}', expected '{expectedType}'.");
            }

            return AssetManifestUtility.RuntimeAssetPath(context, asset);
        }

        private static string ResolveLocKey(KKUIPipelineContext context, string locKey)
        {
            if (string.IsNullOrWhiteSpace(locKey))
            {
                return string.Empty;
            }

            if (context.Strings.Strings.TryGetValue(locKey, out var localizedValues) &&
                localizedValues.TryGetValue(context.Strings.DefaultCulture, out var value))
            {
                return value;
            }

            return locKey;
        }

        private static TMP_FontAsset LoadDefaultFontAsset()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        private static void EnsureTextFonts(GameObject root)
        {
            var defaultFont = LoadDefaultFontAsset();
            foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.font != null)
                {
                    continue;
                }

                if (defaultFont == null)
                {
                    throw new InvalidOperationException($"Text '{text.name}' has no TMP font asset and no default font asset is available.");
                }

                text.font = defaultFont;
            }
        }

        private static Type FindType(string ns, string className)
        {
            var fullName = $"{ns}.{className}";
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string ToControlId(string fieldName)
        {
            var trimmed = fieldName.TrimStart('_');
            return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        private static string ToFieldName(string controlId)
        {
            return "_" + char.ToLowerInvariant(controlId[0]) + controlId.Substring(1);
        }

        private static bool IsBindableControl(string type)
        {
            return type == "Text" ||
                type == "Image" ||
                type == "Button" ||
                type == "RawImage" ||
                type == "Toggle" ||
                type == "Slider" ||
                type == "InputField" ||
                type == "Dropdown" ||
                type == "Scrollbar" ||
                type == "ScrollView" ||
                type == "VerticalList";
        }

        private static class ValidatorUtilityProxy
        {
            public static void Walk(UiLayoutNode node, Action<UiLayoutNode> visitor)
            {
                if (node == null)
                {
                    return;
                }

                visitor(node);
                foreach (var child in node.Children ?? new List<UiLayoutNode>())
                {
                    Walk(child, visitor);
                }
            }
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(fullPath).Replace(projectRoot + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');
        }

        private static void EnsureCanOverwrite(string prefabPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing == null)
            {
                return;
            }

            if (existing.GetComponent<GeneratedAssetMarker>() == null)
            {
                throw new InvalidOperationException($"Refusing to overwrite prefab '{prefabPath}' without GeneratedAssetMarker.");
            }
        }

        private static void RegisterAddressable(string prefabPath, string address, string groupName)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null || settings.DefaultGroup == null)
            {
                throw new InvalidOperationException("Addressables settings or default group is not available.");
            }

            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                throw new InvalidOperationException($"Cannot register addressable for '{prefabPath}' because it has no asset guid.");
            }

            var group = GetOrCreateAddressablesGroup(settings, groupName, prefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(address, false);
            settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }

        private static UnityEditor.AddressableAssets.Settings.AddressableAssetGroup GetOrCreateAddressablesGroup(
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings,
            string groupName,
            string prefabPath)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return settings.DefaultGroup;
            }

            var group = settings.FindGroup(groupName);
            if (group != null)
            {
                return group;
            }

            if (settings.DefaultGroup == null)
            {
                throw new InvalidOperationException($"Cannot create Addressables group '{groupName}' for '{prefabPath}' because DefaultGroup is not available.");
            }

            return settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
        }
    }
}
