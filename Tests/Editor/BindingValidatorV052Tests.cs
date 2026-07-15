using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class BindingValidatorV052Tests
    {
        [Test]
        public void UnsupportedPropertyFails()
        {
            var context = CreateContext();
            context.Bindings.Bindings[0].Property = "text";

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BND008"), Is.True);
        }

        [Test]
        public void UnsupportedEventFails()
        {
            var context = CreateContext();
            context.Bindings.Events[0].Event = "onClick";

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BND005"), Is.True);
        }

        [Test]
        public void TextLocKeyAndTextBindingFails()
        {
            var context = CreateTextContextWithLocKeyBinding();

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "TXT003"), Is.True);
        }

        [Test]
        public void StaticButtonTextBindingWarns()
        {
            var context = CreateButtonTextContextWithLocKeyBinding();

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "TXT004" && issue.Severity == KKUIPipelineIssueSeverity.Warning), Is.True);
        }

        [Test]
        public void VerticalListItemsFieldMustBePayloadList()
        {
            var context = CreateContext();
            context.Bindings.Mvvm.Fields.Add(new UiViewModelFieldSpec { Id = "Items", Type = "string" });
            context.Layout.Root.Children.Add(new UiLayoutNode
            {
                Type = "VerticalList",
                Id = "InventoryList",
                VerticalList = new UiVerticalListSpec
                {
                    ItemSourceField = "Items",
                    ItemTemplate = new UiLayoutNode
                    {
                        Type = "Button",
                        Id = "ItemButton",
                        Rect = new UiRectSpec()
                    }
                }
            });

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BND013"), Is.True);
        }

        [Test]
        public void ImageColorAndAlphaBindingsPass()
        {
            var context = CreateImageContext();

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BND008"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "BND009"), Is.False);
        }

        [Test]
        public void CommonGraphicBindingsPass()
        {
            var context = CreateCommonGraphicContext();

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BND008"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "BND009"), Is.False);
        }

        [Test]
        public void ImageColorRequiresColorOrStringField()
        {
            var context = CreateImageContext();
            context.Bindings.Mvvm.Fields[0].Type = "float";

            new BindingValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "BND009"), Is.True);
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
                        Children = new List<UiLayoutNode>
                        {
                            new UiLayoutNode { Type = "Slider", Id = "VolumeSlider" }
                        }
                    }
                },
                Bindings = new UiBindingsManifest
                {
                    Mvvm = new UiMvvmSection
                    {
                        Fields = new List<UiViewModelFieldSpec>
                        {
                            new UiViewModelFieldSpec { Id = "Volume", Type = "float" }
                        }
                    },
                    Bindings = new List<UiBindingSpec>
                    {
                        new UiBindingSpec
                        {
                            ControlId = "VolumeSlider",
                            FieldId = "Volume",
                            Mode = "OneWay",
                            Property = "value"
                        }
                    },
                    Events = new List<UiEventSpec>
                    {
                        new UiEventSpec
                        {
                            ControlId = "VolumeSlider",
                            Event = "onValueChanged",
                            Handler = "OnVolumeChanged"
                        }
                    }
                }
            };
        }

        private static KKUIPipelineContext CreateTextContextWithLocKeyBinding()
        {
            return new KKUIPipelineContext
            {
                Layout = new UiLayoutManifest
                {
                    Root = new UiLayoutNode
                    {
                        Type = "Panel",
                        Id = "Root",
                        Children = new List<UiLayoutNode>
                        {
                            new UiLayoutNode
                            {
                                Type = "Text",
                                Id = "TitleText",
                                Text = new UiTextSpec { LocKey = "title.main" }
                            }
                        }
                    }
                },
                Bindings = new UiBindingsManifest
                {
                    Mvvm = new UiMvvmSection
                    {
                        Fields = new List<UiViewModelFieldSpec>
                        {
                            new UiViewModelFieldSpec { Id = "Title", Type = "string" }
                        }
                    },
                    Bindings = new List<UiBindingSpec>
                    {
                        new UiBindingSpec
                        {
                            ControlId = "TitleText",
                            FieldId = "Title",
                            Mode = "OneWay",
                            Property = "text"
                        }
                    },
                    Events = new List<UiEventSpec>()
                }
            };
        }

        private static KKUIPipelineContext CreateImageContext()
        {
            return new KKUIPipelineContext
            {
                Layout = new UiLayoutManifest
                {
                    Root = new UiLayoutNode
                    {
                        Type = "Panel",
                        Id = "Root",
                        Children = new List<UiLayoutNode>
                        {
                            new UiLayoutNode { Type = "Image", Id = "HealthFill" }
                        }
                    }
                },
                Bindings = new UiBindingsManifest
                {
                    Mvvm = new UiMvvmSection
                    {
                        Fields = new List<UiViewModelFieldSpec>
                        {
                            new UiViewModelFieldSpec { Id = "HealthColor", Type = "Color" },
                            new UiViewModelFieldSpec { Id = "HealthAlpha", Type = "float" },
                            new UiViewModelFieldSpec { Id = "HealthFill", Type = "float" },
                            new UiViewModelFieldSpec { Id = "HealthRaycast", Type = "bool" }
                        }
                    },
                    Bindings = new List<UiBindingSpec>
                    {
                        new UiBindingSpec
                        {
                            ControlId = "HealthFill",
                            FieldId = "HealthColor",
                            Mode = "OneWay",
                            Property = "color"
                        },
                        new UiBindingSpec
                        {
                            ControlId = "HealthFill",
                            FieldId = "HealthAlpha",
                            Mode = "OneWay",
                            Property = "alpha"
                        },
                        new UiBindingSpec
                        {
                            ControlId = "HealthFill",
                            FieldId = "HealthFill",
                            Mode = "OneWay",
                            Property = "fillAmount"
                        },
                        new UiBindingSpec
                        {
                            ControlId = "HealthFill",
                            FieldId = "HealthRaycast",
                            Mode = "OneWay",
                            Property = "raycastTarget"
                        }
                    },
                    Events = new List<UiEventSpec>()
                }
            };
        }

        private static KKUIPipelineContext CreateCommonGraphicContext()
        {
            return new KKUIPipelineContext
            {
                Layout = new UiLayoutManifest
                {
                    Root = new UiLayoutNode
                    {
                        Type = "Panel",
                        Id = "Root",
                        Children = new List<UiLayoutNode>
                        {
                            new UiLayoutNode { Type = "Panel", Id = "PanelRoot", Image = new UiImageSpec { Color = "#FFFFFF" } },
                            new UiLayoutNode { Type = "Text", Id = "TitleText" },
                            new UiLayoutNode { Type = "RawImage", Id = "PreviewImage" },
                            new UiLayoutNode { Type = "Button", Id = "ActionButton" },
                            new UiLayoutNode { Type = "Slider", Id = "ProgressSlider" },
                            new UiLayoutNode { Type = "Scrollbar", Id = "ScrollBar" }
                        }
                    }
                },
                Bindings = new UiBindingsManifest
                {
                    Mvvm = new UiMvvmSection
                    {
                        Fields = new List<UiViewModelFieldSpec>
                        {
                            new UiViewModelFieldSpec { Id = "PanelAlpha", Type = "float" },
                            new UiViewModelFieldSpec { Id = "TitleColor", Type = "string" },
                            new UiViewModelFieldSpec { Id = "TitleSize", Type = "int" },
                            new UiViewModelFieldSpec { Id = "PreviewAlpha", Type = "float" },
                            new UiViewModelFieldSpec { Id = "ButtonColor", Type = "Color" },
                            new UiViewModelFieldSpec { Id = "SliderMax", Type = "float" },
                            new UiViewModelFieldSpec { Id = "ScrollbarSize", Type = "float" }
                        }
                    },
                    Bindings = new List<UiBindingSpec>
                    {
                        new UiBindingSpec { ControlId = "PanelRoot", FieldId = "PanelAlpha", Mode = "OneWay", Property = "alpha" },
                        new UiBindingSpec { ControlId = "TitleText", FieldId = "TitleColor", Mode = "OneWay", Property = "color" },
                        new UiBindingSpec { ControlId = "TitleText", FieldId = "TitleSize", Mode = "OneWay", Property = "fontSize" },
                        new UiBindingSpec { ControlId = "PreviewImage", FieldId = "PreviewAlpha", Mode = "OneWay", Property = "alpha" },
                        new UiBindingSpec { ControlId = "ActionButton", FieldId = "ButtonColor", Mode = "OneWay", Property = "color" },
                        new UiBindingSpec { ControlId = "ProgressSlider", FieldId = "SliderMax", Mode = "OneWay", Property = "maxValue" },
                        new UiBindingSpec { ControlId = "ScrollBar", FieldId = "ScrollbarSize", Mode = "OneWay", Property = "size" }
                    },
                    Events = new List<UiEventSpec>()
                }
            };
        }

        private static KKUIPipelineContext CreateButtonTextContextWithLocKeyBinding()
        {
            var context = CreateTextContextWithLocKeyBinding();
            context.Layout.Root.Children[0] = new UiLayoutNode
            {
                Type = "Button",
                Id = "ConfirmButton",
                Children = new List<UiLayoutNode>
                {
                    new UiLayoutNode
                    {
                        Type = "Text",
                        Id = "TitleText",
                        Text = new UiTextSpec { LocKey = "title.main" }
                    }
                }
            };
            return context;
        }
    }
}
