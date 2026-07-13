using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Internal;

namespace KK.UI.UMG.Editor.Pipeline
{
    public sealed class PrefabLayoutExporter
    {
        private const int FloatPrecision = 6;
        private const double FloatEpsilon = 0.000001d;
        private const string JsonNumberPattern = @"-?(?:\d+(?:\.\d+)?|\.\d+)(?:[eE][+-]?\d+)?";

        private static readonly Regex InlineAnchorRegex = new Regex(
            @"""anchorMin""\s*:\s*\[[^\r\n\]]+,[^\r\n\]]+\]",
            RegexOptions.CultureInvariant);

        private static readonly Regex NumericPairRegex = new Regex(
            @"\[\s*(" + JsonNumberPattern + @")\s*,\s*(" + JsonNumberPattern + @")\s*\]",
            RegexOptions.CultureInvariant);

        public IReadOnlyList<string> Export(KKUIPipelineContext context)
        {
            if (context?.Package?.Manifests == null)
            {
                throw new ArgumentException("A loaded pipeline context is required.", nameof(context));
            }

            var layoutPath = Path.GetFullPath(Path.Combine(context.SourceRoot, context.Package.Manifests.Layout));
            if (!File.Exists(layoutPath))
            {
                throw new FileNotFoundException("layout.json does not exist.", layoutPath);
            }

            var layoutAssetPath = AssetManifestUtility.ToAssetPath(layoutPath);
            var prefabPath = AssetManifestUtility.ToAssetPath(
                Path.Combine(context.GeneratedRoot, "Prefabs", $"{context.Package.PackageId}View.prefab"));
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                throw new FileNotFoundException($"Generated prefab does not exist. Run Generate first: {prefabPath}", prefabPath);
            }

            var marker = prefabAsset.GetComponent<GeneratedAssetMarker>();
            if (marker == null || !string.Equals(marker.PackageId, context.Package.PackageId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Prefab '{prefabPath}' is not generated for package '{context.Package.PackageId}'.");
            }

            var originalText = File.ReadAllText(layoutPath);
            var document = JObject.Parse(originalText);
            var originalDocument = document.DeepClone();
            var sourceRoot = document["root"] as JObject
                ?? throw new InvalidOperationException("layout.json root must be an object.");
            var changedNodes = new List<string>();

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                CaptureNode(sourceRoot, prefabRoot.transform, true, "root", changedNodes);
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            if (JToken.DeepEquals(originalDocument, document))
            {
                return changedNodes;
            }

            if (!AssetDatabase.MakeEditable(layoutAssetPath))
            {
                throw new UnauthorizedAccessException($"layout.json is not editable: {layoutAssetPath}");
            }

            File.WriteAllText(layoutPath, FormatJson(document, originalText), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(layoutAssetPath, ImportAssetOptions.ForceUpdate);
            return changedNodes;
        }

        private static void CaptureNode(
            JObject sourceNode,
            Transform generatedNode,
            bool isRoot,
            string sourcePath,
            List<string> changedNodes)
        {
            var nodeId = sourceNode.Value<string>("id");
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new InvalidOperationException($"Source node at '{sourcePath}' has no id.");
            }

            var rectBefore = sourceNode["rect"]?.DeepClone();
            var layoutBefore = sourceNode["layoutComponents"]?.DeepClone();

            if (!isRoot)
            {
                CaptureRect(sourceNode, generatedNode, sourcePath);
            }

            CaptureLayoutComponents(sourceNode, generatedNode.gameObject, sourcePath);
            if (!TokensEqual(rectBefore, sourceNode["rect"]) ||
                !TokensEqual(layoutBefore, sourceNode["layoutComponents"]))
            {
                changedNodes.Add(sourcePath);
            }

            var generatedChildrenParent = ResolveGeneratedChildrenParent(sourceNode, generatedNode, sourcePath);
            Transform itemTemplate = null;
            if (sourceNode["verticalList"]?["itemTemplate"] is JObject sourceItemTemplate)
            {
                itemTemplate = FindDirectChild(generatedChildrenParent, "ItemTemplate", null, false);
                if (itemTemplate == null)
                {
                    throw new InvalidOperationException(
                        $"Generated item template for Source node '{sourcePath}' was not found. Save the prefab without renaming generated nodes.");
                }

                CaptureNode(
                    sourceItemTemplate,
                    itemTemplate,
                    false,
                    $"{sourcePath}/verticalList/itemTemplate",
                    changedNodes);
            }

            if (!(sourceNode["children"] is JArray children))
            {
                return;
            }

            foreach (var childToken in children)
            {
                if (!(childToken is JObject sourceChild))
                {
                    throw new InvalidOperationException($"Source node '{sourcePath}' contains a non-object child.");
                }

                var childId = sourceChild.Value<string>("id");
                var generatedChild = FindDirectChild(generatedChildrenParent, childId, itemTemplate, true);
                if (generatedChild == null)
                {
                    throw new InvalidOperationException(
                        $"Generated node '{childId}' for Source node '{sourcePath}' was not found. Save the prefab without renaming or reparenting generated nodes.");
                }

                CaptureNode(sourceChild, generatedChild, false, $"{sourcePath}/children/{childId}", changedNodes);
            }
        }

        private static Transform ResolveGeneratedChildrenParent(JObject sourceNode, Transform generatedNode, string sourcePath)
        {
            var type = sourceNode.Value<string>("type");
            if (type != "ScrollView" && type != "VerticalList")
            {
                return generatedNode;
            }

            var scrollRect = generatedNode.GetComponent<ScrollRect>();
            if (scrollRect == null || scrollRect.content == null)
            {
                throw new InvalidOperationException($"Generated node '{sourcePath}' has no ScrollRect content.");
            }

            return scrollRect.content;
        }

        private static Transform FindDirectChild(Transform parent, string name, Transform excluded, bool preferLast)
        {
            if (parent == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            Transform match = null;
            foreach (Transform child in parent)
            {
                if (child != excluded && string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    match = child;
                    if (!preferLast)
                    {
                        return match;
                    }
                }
            }

            return match;
        }

        private static void CaptureRect(JObject sourceNode, Transform generatedNode, string sourcePath)
        {
            var sourceRect = sourceNode["rect"] as JObject
                ?? throw new InvalidOperationException($"Source node '{sourcePath}' has no rect object.");
            var rect = generatedNode as RectTransform ?? generatedNode.GetComponent<RectTransform>();
            if (rect == null)
            {
                throw new InvalidOperationException($"Generated node '{sourcePath}' has no RectTransform.");
            }

            sourceRect["anchorMin"] = VectorArray(rect.anchorMin);
            sourceRect["anchorMax"] = VectorArray(rect.anchorMax);
            sourceRect["position"] = VectorArray(rect.anchoredPosition);
            sourceRect[sourceRect["size"] == null && sourceRect["sizeDelta"] != null ? "sizeDelta" : "size"] = VectorArray(rect.sizeDelta);
        }

        private static void CaptureLayoutComponents(JObject sourceNode, GameObject generatedNode, string sourcePath)
        {
            if (!(sourceNode["layoutComponents"] is JObject sourceComponents))
            {
                return;
            }

            if (sourceComponents["layoutElement"] is JObject layoutElement)
            {
                var component = RequireComponent<LayoutElement>(generatedNode, sourcePath, "layoutElement");
                Set(layoutElement, "ignoreLayout", new JValue(component.ignoreLayout), new JValue(false));
                Set(layoutElement, "minWidth", Number(component.minWidth), Number(-1f));
                Set(layoutElement, "minHeight", Number(component.minHeight), Number(-1f));
                Set(layoutElement, "preferredWidth", Number(component.preferredWidth), Number(-1f));
                Set(layoutElement, "preferredHeight", Number(component.preferredHeight), Number(-1f));
                Set(layoutElement, "flexibleWidth", Number(component.flexibleWidth), Number(-1f));
                Set(layoutElement, "flexibleHeight", Number(component.flexibleHeight), Number(-1f));
                Set(layoutElement, "layoutPriority", new JValue(component.layoutPriority), new JValue(1));
            }

            if (sourceComponents["horizontalLayout"] is JObject horizontalLayout)
            {
                CaptureLayoutGroup(
                    horizontalLayout,
                    RequireComponent<HorizontalLayoutGroup>(generatedNode, sourcePath, "horizontalLayout"));
            }

            if (sourceComponents["verticalLayout"] is JObject verticalLayout)
            {
                CaptureLayoutGroup(
                    verticalLayout,
                    RequireComponent<VerticalLayoutGroup>(generatedNode, sourcePath, "verticalLayout"));
            }

            if (sourceComponents["gridLayout"] is JObject gridLayout)
            {
                var component = RequireComponent<GridLayoutGroup>(generatedNode, sourcePath, "gridLayout");
                CapturePadding(gridLayout, component.padding);
                Set(gridLayout, "childAlignment", new JValue(component.childAlignment.ToString()), new JValue("UpperLeft"));
                CaptureVector(gridLayout, "cellSize", component.cellSize, new Vector2(100f, 100f));
                CaptureVector(gridLayout, "spacing", component.spacing, Vector2.zero);
                Set(gridLayout, "startCorner", new JValue(component.startCorner.ToString()), new JValue("UpperLeft"));
                Set(gridLayout, "startAxis", new JValue(component.startAxis.ToString()), new JValue("Horizontal"));
                Set(gridLayout, "constraint", new JValue(component.constraint.ToString()), new JValue("Flexible"));
                Set(gridLayout, "constraintCount", new JValue(component.constraintCount), new JValue(0));
            }

            if (sourceComponents["contentSizeFitter"] is JObject contentSizeFitter)
            {
                var component = RequireComponent<ContentSizeFitter>(generatedNode, sourcePath, "contentSizeFitter");
                Set(contentSizeFitter, "horizontalFit", new JValue(component.horizontalFit.ToString()), new JValue("Unconstrained"));
                Set(contentSizeFitter, "verticalFit", new JValue(component.verticalFit.ToString()), new JValue("Unconstrained"));
            }

            if (sourceComponents["aspectRatioFitter"] is JObject aspectRatioFitter)
            {
                var component = RequireComponent<AspectRatioFitter>(generatedNode, sourcePath, "aspectRatioFitter");
                Set(aspectRatioFitter, "aspectMode", new JValue(component.aspectMode.ToString()), new JValue("FitInParent"));
                Set(aspectRatioFitter, "aspectRatio", Number(component.aspectRatio), Number(1f));
            }
        }

        private static T RequireComponent<T>(GameObject gameObject, string sourcePath, string sourceName) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Generated node '{sourcePath}' is missing the declared {sourceName} component.");
            }

            return component;
        }

        private static void CaptureLayoutGroup(JObject source, HorizontalOrVerticalLayoutGroup component)
        {
            CapturePadding(source, component.padding);
            Set(source, "spacing", Number(component.spacing), Number(0f));
            Set(source, "childAlignment", new JValue(component.childAlignment.ToString()), new JValue("UpperLeft"));
            Set(source, "childControlWidth", new JValue(component.childControlWidth), new JValue(true));
            Set(source, "childControlHeight", new JValue(component.childControlHeight), new JValue(true));
            Set(source, "childForceExpandWidth", new JValue(component.childForceExpandWidth), new JValue(false));
            Set(source, "childForceExpandHeight", new JValue(component.childForceExpandHeight), new JValue(false));
            Set(source, "childScaleWidth", new JValue(component.childScaleWidth), new JValue(false));
            Set(source, "childScaleHeight", new JValue(component.childScaleHeight), new JValue(false));
            Set(source, "reverseArrangement", new JValue(component.reverseArrangement), new JValue(false));
        }

        private static void CapturePadding(JObject source, RectOffset value)
        {
            var padding = source["padding"] as JObject;
            if (padding == null && value.left == 0 && value.right == 0 && value.top == 0 && value.bottom == 0)
            {
                return;
            }

            if (padding == null)
            {
                padding = new JObject();
                source["padding"] = padding;
            }

            Set(padding, "left", new JValue(value.left), new JValue(0));
            Set(padding, "right", new JValue(value.right), new JValue(0));
            Set(padding, "top", new JValue(value.top), new JValue(0));
            Set(padding, "bottom", new JValue(value.bottom), new JValue(0));
        }

        private static void CaptureVector(JObject source, string name, Vector2 value, Vector2 defaultValue)
        {
            var vector = source[name] as JObject;
            if (vector == null && Approximately(value, defaultValue))
            {
                return;
            }

            if (vector == null)
            {
                vector = new JObject();
                source[name] = vector;
            }

            Set(vector, "x", Number(value.x), Number(defaultValue.x));
            Set(vector, "y", Number(value.y), Number(defaultValue.y));
        }

        private static void Set(JObject target, string name, JToken value, JToken defaultValue)
        {
            if (target.Property(name) != null || !JToken.DeepEquals(value, defaultValue))
            {
                target[name] = value;
            }
        }

        private static JArray VectorArray(Vector2 value)
        {
            return new JArray(Number(value.x), Number(value.y));
        }

        private static JValue Number(float value)
        {
            var rounded = Math.Round(value, FloatPrecision, MidpointRounding.AwayFromZero);
            if (Math.Abs(rounded) < FloatEpsilon)
            {
                rounded = 0d;
            }

            var whole = Math.Round(rounded);
            return Math.Abs(rounded - whole) < FloatEpsilon
                ? new JValue((long)whole)
                : new JValue(rounded);
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Math.Abs(left.x - right.x) < FloatEpsilon && Math.Abs(left.y - right.y) < FloatEpsilon;
        }

        private static bool TokensEqual(JToken left, JToken right)
        {
            return left == null ? right == null : right != null && JToken.DeepEquals(left, right);
        }

        private static string FormatJson(JObject document, string originalText)
        {
            var formatted = document.ToString(Formatting.Indented);
            if (InlineAnchorRegex.IsMatch(originalText))
            {
                formatted = NumericPairRegex.Replace(formatted, "[$1, $2]");
            }

            var newline = originalText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            formatted = formatted.Replace("\r\n", "\n");
            if (newline != "\n")
            {
                formatted = formatted.Replace("\n", newline);
            }

            var hadTrailingNewline = originalText.EndsWith("\n", StringComparison.Ordinal);
            return hadTrailingNewline ? formatted + newline : formatted;
        }
    }
}
