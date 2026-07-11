using System;
using System.Collections.Generic;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class LayoutValidator : IManifestValidator
    {
        private static readonly HashSet<string> TextAnchors = new HashSet<string>(StringComparer.Ordinal)
        {
            "UpperLeft",
            "UpperCenter",
            "UpperRight",
            "MiddleLeft",
            "MiddleCenter",
            "MiddleRight",
            "LowerLeft",
            "LowerCenter",
            "LowerRight"
        };

        private static readonly HashSet<string> GridStartCorners = new HashSet<string>(StringComparer.Ordinal)
        {
            "UpperLeft",
            "UpperRight",
            "LowerLeft",
            "LowerRight"
        };

        private static readonly HashSet<string> GridStartAxes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Horizontal",
            "Vertical"
        };

        private static readonly HashSet<string> GridConstraints = new HashSet<string>(StringComparer.Ordinal)
        {
            "Flexible",
            "FixedColumnCount",
            "FixedRowCount"
        };

        private static readonly HashSet<string> FitModes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Unconstrained",
            "MinSize",
            "PreferredSize"
        };

        private static readonly HashSet<string> AspectModes = new HashSet<string>(StringComparer.Ordinal)
        {
            "None",
            "WidthControlsHeight",
            "HeightControlsWidth",
            "FitInParent",
            "EnvelopeParent"
        };

        public void Validate(KKUIPipelineContext context)
        {
            if (context.Layout.Root == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY001", "layout root is required.");
                return;
            }

            var ids = new HashSet<string>();
            ValidatorUtility.Walk(context.Layout.Root, node =>
            {
                ValidateNode(context, ids, node, node == context.Layout.Root);
            });
            ValidateLayoutElementParentOwnership(context, context.Layout.Root, false);
        }

        private static void ValidateNode(KKUIPipelineContext context, HashSet<string> ids, UiLayoutNode node, bool isRoot)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY002", "Every layout node must have an id.");
                return;
            }

            if (!ids.Add(node.Id))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY003", $"Duplicate layout id '{node.Id}'.");
            }

            if (!ValidatorUtility.SupportedControls.Contains(node.Type))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY004", $"Unsupported layout type '{node.Type}' for '{node.Id}'.");
            }

            if (!isRoot && node.Rect == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY005", $"Non-root node '{node.Id}' must define rect.");
            }

            if (node.Type == "Text" && node.Text == null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY006", $"Text node '{node.Id}' must define text.");
            }

            if (node.Type == "Text" &&
                !string.IsNullOrWhiteSpace(node.Text?.Alignment) &&
                !TextAnchors.Contains(node.Text.Alignment))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY067", $"Text node '{node.Id}' alignment '{node.Text.Alignment}' is not supported.");
            }

            if (node.Type == "VerticalList" && node.VerticalList?.ItemTemplate != null)
            {
                ValidateItemTemplate(context, node);
            }

            ValidateLayoutComponents(context, node);
        }

        private static void ValidateItemTemplate(KKUIPipelineContext context, UiLayoutNode listNode)
        {
            var ids = new HashSet<string>();
            ValidatorUtility.Walk(listNode.VerticalList.ItemTemplate, itemNode =>
            {
                ValidateNode(context, ids, itemNode, false);
            });
        }

        private static void ValidateLayoutComponents(KKUIPipelineContext context, UiLayoutNode node)
        {
            var layout = node.LayoutComponents;
            if (layout == null)
            {
                return;
            }

            var layoutGroupCount = 0;
            if (layout.HorizontalLayout != null)
            {
                layoutGroupCount++;
                ValidateLayoutGroup(context, node, layout.HorizontalLayout, "horizontalLayout");
            }

            if (layout.VerticalLayout != null)
            {
                layoutGroupCount++;
                ValidateLayoutGroup(context, node, layout.VerticalLayout, "verticalLayout");
            }

            if (layout.GridLayout != null)
            {
                layoutGroupCount++;
                ValidateGridLayout(context, node, layout.GridLayout);
            }

            if (layoutGroupCount > 1)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY050", $"Node '{node.Id}' can define only one layout group.");
            }

            if (layoutGroupCount > 0 && (node.Children == null || node.Children.Count == 0))
            {
                context.Add(KKUIPipelineIssueSeverity.Warning, "LAY051", $"Node '{node.Id}' defines a layout group but has no children.");
            }

            if (layout.ContentSizeFitter != null)
            {
                ValidateContentSizeFitter(context, node, layout.ContentSizeFitter);
                if (layoutGroupCount > 0)
                {
                    context.Add(KKUIPipelineIssueSeverity.Warning, "LAY052", $"Node '{node.Id}' combines a layout group with ContentSizeFitter. Check for layout ownership loops.");
                }
            }

            if (layout.AspectRatioFitter != null)
            {
                ValidateAspectRatioFitter(context, node, layout.AspectRatioFitter);
            }

            if (layout.ContentSizeFitter != null && layout.AspectRatioFitter != null)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY053", $"Node '{node.Id}' cannot define both contentSizeFitter and aspectRatioFitter.");
            }

            if (layout.LayoutElement != null)
            {
                ValidateLayoutElement(context, node, layout.LayoutElement);
            }
        }

        private static void ValidateLayoutGroup(KKUIPipelineContext context, UiLayoutNode node, UiLayoutGroupSpec spec, string name)
        {
            if (!TextAnchors.Contains(spec.ChildAlignment))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY054", $"Node '{node.Id}' {name}.childAlignment '{spec.ChildAlignment}' is not supported.");
            }
        }

        private static void ValidateGridLayout(KKUIPipelineContext context, UiLayoutNode node, UiGridLayoutSpec spec)
        {
            if (!TextAnchors.Contains(spec.ChildAlignment))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY055", $"Node '{node.Id}' gridLayout.childAlignment '{spec.ChildAlignment}' is not supported.");
            }

            if (!GridStartCorners.Contains(spec.StartCorner))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY056", $"Node '{node.Id}' gridLayout.startCorner '{spec.StartCorner}' is not supported.");
            }

            if (!GridStartAxes.Contains(spec.StartAxis))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY057", $"Node '{node.Id}' gridLayout.startAxis '{spec.StartAxis}' is not supported.");
            }

            if (!GridConstraints.Contains(spec.Constraint))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY058", $"Node '{node.Id}' gridLayout.constraint '{spec.Constraint}' is not supported.");
            }

            if (spec.CellSize == null || spec.CellSize.X <= 0f || spec.CellSize.Y <= 0f)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY059", $"Node '{node.Id}' gridLayout.cellSize must be greater than zero.");
            }

            if ((spec.Constraint == "FixedColumnCount" || spec.Constraint == "FixedRowCount") && spec.ConstraintCount <= 0)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY060", $"Node '{node.Id}' gridLayout.constraintCount must be greater than zero for '{spec.Constraint}'.");
            }
        }

        private static void ValidateContentSizeFitter(KKUIPipelineContext context, UiLayoutNode node, UiContentSizeFitterSpec spec)
        {
            if (!FitModes.Contains(spec.HorizontalFit))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY061", $"Node '{node.Id}' contentSizeFitter.horizontalFit '{spec.HorizontalFit}' is not supported.");
            }

            if (!FitModes.Contains(spec.VerticalFit))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY062", $"Node '{node.Id}' contentSizeFitter.verticalFit '{spec.VerticalFit}' is not supported.");
            }
        }

        private static void ValidateAspectRatioFitter(KKUIPipelineContext context, UiLayoutNode node, UiAspectRatioFitterSpec spec)
        {
            if (!AspectModes.Contains(spec.AspectMode))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY063", $"Node '{node.Id}' aspectRatioFitter.aspectMode '{spec.AspectMode}' is not supported.");
            }

            if (spec.AspectRatio <= 0f)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LAY064", $"Node '{node.Id}' aspectRatioFitter.aspectRatio must be greater than zero.");
            }
        }

        private static void ValidateLayoutElement(KKUIPipelineContext context, UiLayoutNode node, UiLayoutElementSpec spec)
        {
            if (spec.PreferredWidth == 0f || spec.PreferredHeight == 0f)
            {
                context.Add(KKUIPipelineIssueSeverity.Warning, "LAY065", $"Node '{node.Id}' layoutElement preferred size is 0.");
            }
        }

        private static bool HasAnyLayoutGroup(UiLayoutComponentsSpec layout)
        {
            return layout != null && (layout.HorizontalLayout != null || layout.VerticalLayout != null || layout.GridLayout != null);
        }

        private static void ValidateLayoutElementParentOwnership(KKUIPipelineContext context, UiLayoutNode node, bool parentHasLayoutGroup)
        {
            if (node == null)
            {
                return;
            }

            var layoutElement = node.LayoutComponents?.LayoutElement;
            if (layoutElement != null && (layoutElement.FlexibleWidth > 0f || layoutElement.FlexibleHeight > 0f) && !parentHasLayoutGroup)
            {
                context.Add(KKUIPipelineIssueSeverity.Warning, "LAY066", $"Node '{node.Id}' layoutElement uses flexible size, but its parent has no layout group.");
            }

            var currentHasLayoutGroup = HasAnyLayoutGroup(node.LayoutComponents);
            foreach (var child in node.Children ?? new List<UiLayoutNode>())
            {
                ValidateLayoutElementParentOwnership(context, child, currentHasLayoutGroup);
            }

            if (node.Type == "VerticalList" && node.VerticalList?.ItemTemplate != null)
            {
                ValidateLayoutElementParentOwnership(context, node.VerticalList.ItemTemplate, true);
            }
        }
    }
}
