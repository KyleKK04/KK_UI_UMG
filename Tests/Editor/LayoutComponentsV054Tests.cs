using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Editor.Generators;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class LayoutComponentsV054Tests
    {
        [Test]
        public void LayoutComponentsManifestParses()
        {
            var context = KKUIPipelineContext.Load("Assets/UI/Source/LayoutComponentsGallery/package.json");
            var toolbar = context.Layout.Root.Children[0].Children[0];

            Assert.That(toolbar.LayoutComponents.HorizontalLayout.Spacing, Is.EqualTo(10f));
            Assert.That(toolbar.LayoutComponents.HorizontalLayout.ChildAlignment, Is.EqualTo("MiddleLeft"));
            Assert.That(toolbar.LayoutComponents.LayoutElement.PreferredHeight, Is.EqualTo(72f));
        }

        [Test]
        public void RejectsMultipleLayoutGroupsOnSameNode()
        {
            var context = CreateContext();
            context.Layout.Root.LayoutComponents = new UiLayoutComponentsSpec
            {
                HorizontalLayout = new UiHorizontalLayoutSpec(),
                VerticalLayout = new UiVerticalLayoutSpec()
            };

            new LayoutValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "LAY050" && issue.Severity == KKUIPipelineIssueSeverity.Error), Is.True);
        }

        [Test]
        public void RejectsInvalidGridConstraintCount()
        {
            var context = CreateContext();
            context.Layout.Root.LayoutComponents = new UiLayoutComponentsSpec
            {
                GridLayout = new UiGridLayoutSpec
                {
                    CellSize = new UiVector2Spec { X = 64f, Y = 64f },
                    Constraint = "FixedColumnCount",
                    ConstraintCount = 0
                }
            };

            new LayoutValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "LAY060" && issue.Severity == KKUIPipelineIssueSeverity.Error), Is.True);
        }

        [Test]
        public void RejectsInvalidAspectRatio()
        {
            var context = CreateContext();
            context.Layout.Root.LayoutComponents = new UiLayoutComponentsSpec
            {
                AspectRatioFitter = new UiAspectRatioFitterSpec
                {
                    AspectMode = "FitInParent",
                    AspectRatio = 0f
                }
            };

            new LayoutValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "LAY064" && issue.Severity == KKUIPipelineIssueSeverity.Error), Is.True);
        }

        [Test]
        public void WarnsLayoutGroupWithoutChildren()
        {
            var context = CreateContext();
            context.Layout.Root.LayoutComponents = new UiLayoutComponentsSpec
            {
                VerticalLayout = new UiVerticalLayoutSpec()
            };

            new LayoutValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "LAY051" && issue.Severity == KKUIPipelineIssueSeverity.Warning), Is.True);
        }

        [Test]
        public void GeneratesHorizontalLayoutGroupAndLayoutElement()
        {
            var gameObject = new GameObject("Toolbar");
            try
            {
                InvokeApplyLayoutComponents(gameObject, new UiLayoutComponentsSpec
                {
                    HorizontalLayout = new UiHorizontalLayoutSpec
                    {
                        Padding = new UiPaddingSpec { Left = 4, Right = 6, Top = 8, Bottom = 10 },
                        Spacing = 12f,
                        ChildAlignment = "MiddleLeft",
                        ChildControlWidth = false,
                        ChildControlHeight = true
                    },
                    LayoutElement = new UiLayoutElementSpec
                    {
                        PreferredWidth = 220f,
                        PreferredHeight = 48f
                    }
                });

                var layout = gameObject.GetComponent<HorizontalLayoutGroup>();
                var element = gameObject.GetComponent<LayoutElement>();

                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.padding.left, Is.EqualTo(4));
                Assert.That(layout.padding.right, Is.EqualTo(6));
                Assert.That(layout.spacing, Is.EqualTo(12f));
                Assert.That(layout.childAlignment, Is.EqualTo(TextAnchor.MiddleLeft));
                Assert.That(layout.childControlWidth, Is.False);
                Assert.That(element.preferredWidth, Is.EqualTo(220f));
                Assert.That(element.preferredHeight, Is.EqualTo(48f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GeneratesGridContentSizeAndAspectComponents()
        {
            var grid = new GameObject("Grid");
            var aspect = new GameObject("Aspect");
            try
            {
                InvokeApplyLayoutComponents(grid, new UiLayoutComponentsSpec
                {
                    GridLayout = new UiGridLayoutSpec
                    {
                        CellSize = new UiVector2Spec { X = 72f, Y = 64f },
                        Spacing = new UiVector2Spec { X = 8f, Y = 10f },
                        Constraint = "FixedColumnCount",
                        ConstraintCount = 3
                    },
                    ContentSizeFitter = new UiContentSizeFitterSpec
                    {
                        HorizontalFit = "Unconstrained",
                        VerticalFit = "PreferredSize"
                    }
                });
                InvokeApplyLayoutComponents(aspect, new UiLayoutComponentsSpec
                {
                    AspectRatioFitter = new UiAspectRatioFitterSpec
                    {
                        AspectMode = "FitInParent",
                        AspectRatio = 1.7778f
                    }
                });

                var gridLayout = grid.GetComponent<GridLayoutGroup>();
                var fitter = grid.GetComponent<ContentSizeFitter>();
                var aspectFitter = aspect.GetComponent<AspectRatioFitter>();

                Assert.That(gridLayout.cellSize, Is.EqualTo(new Vector2(72f, 64f)));
                Assert.That(gridLayout.spacing, Is.EqualTo(new Vector2(8f, 10f)));
                Assert.That(gridLayout.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(gridLayout.constraintCount, Is.EqualTo(3));
                Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
                Assert.That(aspectFitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
                Assert.That(aspectFitter.aspectRatio, Is.EqualTo(1.7778f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(grid);
                Object.DestroyImmediate(aspect);
            }
        }

        private static void InvokeApplyLayoutComponents(GameObject gameObject, UiLayoutComponentsSpec spec)
        {
            var method = typeof(UguiPrefabGenerator).GetMethod("ApplyLayoutComponents", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { gameObject, spec });
        }

        private static KKUIPipelineContext CreateContext()
        {
            return new KKUIPipelineContext
            {
                Layout = new UiLayoutManifest
                {
                    Root = new UiLayoutNode
                    {
                        Type = "Panel",
                        Id = "Root",
                        Children = new List<UiLayoutNode>()
                    }
                }
            };
        }
    }
}
