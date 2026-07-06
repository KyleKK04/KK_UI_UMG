using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class LocKeyValidatorTests
    {
        [Test]
        public void MissingDefaultCultureFails()
        {
            var context = CreateContext();
            context.Strings.DefaultCulture = "";

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "LOC002"), Is.True);
        }

        [Test]
        public void MissingLocKeyFails()
        {
            var context = CreateContext();
            context.Layout.Root.Children[0].Text.LocKey = "missing";

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "TXT002"), Is.True);
        }

        [Test]
        public void ValueAndLocKeyTogetherFail()
        {
            var context = CreateContext();
            context.Layout.Root.Children[0].Text.Value = "literal";

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "LOC006"), Is.True);
        }

        [Test]
        public void LocKeyWithoutBindingPasses()
        {
            var context = CreateContext();
            context.Bindings.Bindings.Clear();

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error), Is.False);
        }

        [Test]
        public void StaticLiteralTextWarns()
        {
            var context = CreateContext();
            context.Layout.Root.Children[0].Text = new UiTextSpec { Value = "literal" };

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "TXT001" && issue.Severity == KKUIPipelineIssueSeverity.Warning), Is.True);
        }

        [Test]
        public void LocKeyMissingDefaultCultureFails()
        {
            var context = CreateContext();
            context.Strings.Strings["message.default"].Remove("zh-Hans");

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "TXT002"), Is.True);
        }

        [Test]
        public void UnusedKeyWarns()
        {
            var context = CreateContext();
            context.Strings.Strings["unused"] = new Dictionary<string, string> { ["zh-Hans"] = "unused" };

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "TXT005" && issue.Severity == KKUIPipelineIssueSeverity.Warning), Is.True);
        }

        [Test]
        public void DuplicateGeneratedStringConstantFails()
        {
            var context = CreateContext();
            context.Strings.Strings["message_default"] = new Dictionary<string, string> { ["zh-Hans"] = "重复" };

            new LocKeyValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "LOC012"), Is.True);
        }

        private static KKUIPipelineContext CreateContext()
        {
            return new KKUIPipelineContext
            {
                Strings = new UiStringsManifest
                {
                    SchemaVersion = "1.0",
                    DefaultCulture = "zh-Hans",
                    Cultures = new List<string> { "zh-Hans", "en-US" },
                    Strings = new Dictionary<string, Dictionary<string, string>>
                    {
                        ["message.default"] = new Dictionary<string, string>
                        {
                            ["zh-Hans"] = "默认",
                            ["en-US"] = "Default"
                        }
                    }
                },
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
                                Id = "MessageText",
                                Text = new UiTextSpec { LocKey = "message.default" }
                            }
                        }
                    }
                },
                Bindings = new UiBindingsManifest
                {
                    Mvvm = new UiMvvmSection
                    {
                        Fields = new List<UiViewModelFieldSpec>()
                    },
                    Bindings = new List<UiBindingSpec>()
                }
            };
        }
    }
}
