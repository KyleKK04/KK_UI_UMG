using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class InventoryPanelV053Tests
    {
        private const string PackagePath = "Assets/UI/Source/InventoryPanel/package.json";
        private const string GeneratedScriptsRoot = "Assets/UI/Generated/InventoryPanel/Scripts";

        [Test]
        public void InventoryPanelSourceValidates()
        {
            var result = new KKUIPipeline().ValidateOnly(PackagePath);

            Assert.That(result.Success, Is.True, string.Join("\n", result.Issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}")));
            Assert.That(result.Issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error), Is.False);
        }

        [Test]
        public void InventoryPanelCoversAllV052Controls()
        {
            var context = KKUIPipelineContext.Load(PackagePath);
            var controls = new HashSet<string>();

            Walk(context.Layout.Root, node => controls.Add(node.Type));

            Assert.That(controls, Is.SupersetOf(new[]
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
            }));
        }

        [Test]
        public void InventoryPanelGeneratedScriptSeedContainsSixFiles()
        {
            var files = Directory.Exists(GeneratedScriptsRoot)
                ? Directory.GetFiles(GeneratedScriptsRoot, "*.Generated.cs")
                : new string[0];

            Assert.That(files.Select(Path.GetFileName), Is.EquivalentTo(new[]
            {
                "InventoryPanelView.Generated.cs",
                "InventoryPanelController.Generated.cs",
                "InventoryPanelViewModel.Generated.cs",
                "InventoryPanelControllerRegistration.Generated.cs",
                "InventoryPanelBus.Generated.cs",
                "InventoryPanelStrings.Generated.cs"
            }));
        }

        private static void Walk(UiLayoutNode node, System.Action<UiLayoutNode> visitor)
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
}
