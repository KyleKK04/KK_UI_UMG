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
