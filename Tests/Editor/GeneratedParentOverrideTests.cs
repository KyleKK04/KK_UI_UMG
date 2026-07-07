using System.IO;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Generators;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class GeneratedParentOverrideTests
    {
        private const string PackageId = "GeneratedParentOverrideTest";
        private const string SourceRoot = "Assets/UI/Source/" + PackageId;
        private const string GeneratedParent = "Assets/UI/GeneratedOverrideTests";

        [SetUp]
        public void SetUp()
        {
            DeleteIfExists(SourceRoot);
            DeleteIfExists(GeneratedParent);
            WriteSourcePackage();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteIfExists(SourceRoot);
            DeleteIfExists(GeneratedParent);
        }

        [Test]
        public void ContextLoadUsesSelectedGeneratedParent()
        {
            var context = KKUIPipelineContext.Load(PackageManifestPath, GeneratedParent);

            Assert.That(context.HasGeneratedParentOverride, Is.True);
            Assert.That(NormalizeFullPath(context.GeneratedRoot), Is.EqualTo(NormalizeFullPath($"{GeneratedParent}/{PackageId}")));
        }

        [Test]
        public void CodegenValidatorAllowsSelectedGeneratedParent()
        {
            var context = KKUIPipelineContext.Load(PackageManifestPath, GeneratedParent);

            new CodegenValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "GEN003"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "GEN008"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "GEN009"), Is.False);
        }

        [Test]
        public void SourceAssetsCopyUnderSelectedGeneratedParent()
        {
            var context = KKUIPipelineContext.Load(PackageManifestPath, GeneratedParent);

            var imported = new UIAssetImporter().Import(context);

            Assert.That(imported, Has.Count.EqualTo(1));
            Assert.That(imported[0], Is.EqualTo($"{GeneratedParent}/{PackageId}/Assets/Images/icon.bytes"));
            Assert.That(File.Exists($"{GeneratedParent}/{PackageId}/Assets/Images/icon.bytes"), Is.True);
        }

        [Test]
        public void PackageSampleSourceAssetsValidate()
        {
            var sourceRoot = Path.GetFullPath("Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Source/KkSampleInventoryPanel");
            var context = new KKUIPipelineContext
            {
                SourceRoot = sourceRoot,
                GeneratedRoot = Path.GetFullPath("Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Generated/KkSampleInventoryPanel"),
                Package = new UiPackageManifest
                {
                    PackageId = "KkSampleInventoryPanel",
                    Namespace = "KK.UI.UMG.Samples.Inventory"
                },
                Assets = new UiAssetsManifest
                {
                    Assets = new System.Collections.Generic.List<UiAssetSpec>
                    {
                        new UiAssetSpec
                        {
                            Id = "image.inventory.panel_bg",
                            Type = "Sprite",
                            Source = "Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Source/KkSampleInventoryPanel/Assets/Images/inventory_panel_bg.png",
                            Target = "../../Generated/KkSampleInventoryPanel/Assets/Images/inventory_panel_bg.png"
                        }
                    }
                },
                Layout = new UiLayoutManifest
                {
                    Root = new UiLayoutNode { Type = "Panel", Id = "Root" }
                }
            };

            new AssetValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "AST004"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "AST005"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "AST006"), Is.False);
        }

        private static string PackageManifestPath => $"{SourceRoot}/package.json";

        private static void WriteSourcePackage()
        {
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory($"{SourceRoot}/Assets/Images");
            File.WriteAllText($"{SourceRoot}/Assets/Images/icon.bytes", "asset");

            File.WriteAllText($"{SourceRoot}/package.json", $@"{{
  ""schemaVersion"": ""1.0"",
  ""packageId"": ""{PackageId}"",
  ""namespace"": ""Game.UI.GeneratedParentOverrideTest"",
  ""version"": ""1.0.0"",
  ""designResolution"": {{ ""width"": 800, ""height"": 600 }},
  ""manifests"": {{
    ""layout"": ""layout.json"",
    ""assets"": ""assets.json"",
    ""bindings"": ""bindings.json"",
    ""codegen"": ""codegen.json"",
    ""strings"": ""strings.json""
  }}
}}");

            File.WriteAllText($"{SourceRoot}/layout.json", @"{
  ""root"": {
    ""type"": ""Panel"",
    ""id"": ""Root"",
    ""rect"": {
      ""anchorMin"": [0, 0],
      ""anchorMax"": [1, 1],
      ""position"": [0, 0],
      ""size"": [0, 0]
    }
  }
}");

            File.WriteAllText($"{SourceRoot}/bindings.json", @"{
  ""mvvm"": { ""fields"": [] },
  ""bindings"": [],
  ""events"": []
}");

            File.WriteAllText($"{SourceRoot}/codegen.json", $@"{{
  ""schemaVersion"": ""1.0"",
  ""namespace"": ""Game.UI.GeneratedParentOverrideTest"",
  ""outputRoot"": ""../../Generated/{PackageId}"",
  ""addressablesKey"": ""UI/{PackageId}/{PackageId}View"",
  ""view"": {{ ""className"": ""{PackageId}View"", ""baseClass"": ""UIViewBase"" }},
  ""controller"": {{ ""className"": ""{PackageId}Controller"", ""baseClass"": ""UIControllerBase"" }},
  ""viewModel"": {{ ""className"": ""{PackageId}ViewModel"" }},
  ""requiredServices"": []
}}");

            File.WriteAllText($"{SourceRoot}/assets.json", $@"{{
  ""assets"": [
    {{
      ""id"": ""icon"",
      ""type"": ""Texture"",
      ""source"": ""{SourceRoot}/Assets/Images/icon.bytes"",
      ""target"": ""../../Generated/{PackageId}/Assets/Images/icon.bytes""
    }}
  ]
}}");

            File.WriteAllText($"{SourceRoot}/strings.json", @"{
  ""schemaVersion"": ""1.0"",
  ""defaultCulture"": ""en"",
  ""cultures"": [""en""],
  ""strings"": {}
}");
        }

        private static string NormalizeFullPath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
        }

        private static void DeleteIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            var meta = path + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
        }
    }
}
