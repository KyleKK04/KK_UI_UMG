using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Internal;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class PrefabLayoutExporterTests
    {
        private const string TestRoot = "Assets/Temp/KKUIPrefabLayoutExporterTests";
        private const string PackageId = "PrefabLayoutTest";

        private static string SourceRoot => $"{TestRoot}/Source/{PackageId}";
        private static string PackageManifestPath => $"{SourceRoot}/package.json";
        private static string GeneratedParent => $"{TestRoot}/Generated";
        private static string GeneratedRoot => $"{TestRoot}/Generated/{PackageId}";
        private static string LayoutPath => $"{SourceRoot}/layout.json";
        private static string PrefabPath => $"{GeneratedRoot}/Prefabs/{PackageId}View.prefab";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void CapturesRectsDeclaredLayoutComponentsAndListTemplate()
        {
            File.WriteAllText(LayoutPath, @"{
  ""root"": {
    ""type"": ""Panel"",
    ""id"": ""Root"",
    ""children"": [
      {
        ""type"": ""Panel"",
        ""id"": ""FreeNode"",
        ""rect"": {
          ""anchorMin"": [0, 0],
          ""anchorMax"": [0, 0],
          ""position"": [0, 0],
          ""size"": [100, 40]
        },
        ""layoutComponents"": {
          ""horizontalLayout"": {
            ""spacing"": 0
          }
        }
      },
      {
        ""type"": ""VerticalList"",
        ""id"": ""List"",
        ""rect"": {
          ""anchorMin"": [0, 0],
          ""anchorMax"": [1, 1],
          ""position"": [0, 0],
          ""size"": [0, 0]
        },
        ""verticalList"": {
          ""itemTemplate"": {
            ""type"": ""Panel"",
            ""id"": ""SourceTemplate"",
            ""rect"": {
              ""anchorMin"": [0, 1],
              ""anchorMax"": [1, 1],
              ""position"": [0, 0],
              ""size"": [0, 40]
            },
            ""children"": [
              {
                ""type"": ""Text"",
                ""id"": ""ItemLabel"",
                ""rect"": {
                  ""anchorMin"": [0, 0],
                  ""anchorMax"": [1, 1],
                  ""position"": [0, 0],
                  ""size"": [0, 0]
                },
                ""text"": {
                  ""value"": ""Item""
                }
              }
            ]
          }
        }
      }
    ]
  }
}
");
            AssetDatabase.ImportAsset(LayoutPath, ImportAssetOptions.ForceSynchronousImport);

            var root = new GameObject($"{PackageId}View", typeof(RectTransform));
            try
            {
                root.AddComponent<GeneratedAssetMarker>().Initialize(PackageId);

                var freeNode = CreateRect("FreeNode", root.transform);
                freeNode.anchorMin = new Vector2(0.1f, 0.2f);
                freeNode.anchorMax = new Vector2(0.7f, 0.8f);
                freeNode.anchoredPosition = new Vector2(12.5f, -7.25f);
                freeNode.sizeDelta = new Vector2(320f, 64f);
                var horizontal = freeNode.gameObject.AddComponent<HorizontalLayoutGroup>();
                horizontal.spacing = 14f;
                horizontal.padding = new RectOffset(3, 5, 7, 9);

                var list = CreateRect("List", root.transform);
                var scrollRect = list.gameObject.AddComponent<ScrollRect>();
                var viewport = CreateRect("Viewport", list);
                var content = CreateRect("Content", viewport);
                scrollRect.viewport = viewport;
                scrollRect.content = content;

                var itemTemplate = CreateRect("ItemTemplate", content);
                itemTemplate.anchorMin = new Vector2(0f, 1f);
                itemTemplate.anchorMax = new Vector2(1f, 1f);
                itemTemplate.anchoredPosition = new Vector2(2f, -3f);
                itemTemplate.sizeDelta = new Vector2(-4f, 88f);
                var itemLabel = CreateRect("ItemLabel", itemTemplate);
                itemLabel.anchoredPosition = new Vector2(20f, 6f);
                itemLabel.sizeDelta = new Vector2(-32f, 26f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            var changedNodes = new PrefabLayoutExporter().Export(CreateContext());
            var output = File.ReadAllText(LayoutPath);

            Assert.That(changedNodes, Does.Contain("root/children/FreeNode"));
            Assert.That(output, Does.Contain("\"anchorMin\": [0.1, 0.2]"));
            Assert.That(output, Does.Contain("\"position\": [12.5, -7.25]"));
            Assert.That(output, Does.Contain("\"size\": [320, 64]"));
            Assert.That(output, Does.Contain("\"spacing\": 14"));
            Assert.That(output, Does.Contain("\"left\": 3"));
            Assert.That(output, Does.Contain("\"size\": [-4, 88]"));
            Assert.That(output, Does.Contain("\"position\": [20, 6]"));
        }

        [Test]
        public void RejectsPrefabFromAnotherPackageWithoutWritingLayout()
        {
            const string layout = "{\"root\":{\"type\":\"Panel\",\"id\":\"Root\"}}";
            File.WriteAllText(LayoutPath, layout);
            AssetDatabase.ImportAsset(LayoutPath, ImportAssetOptions.ForceSynchronousImport);

            var root = new GameObject($"{PackageId}View", typeof(RectTransform));
            try
            {
                root.AddComponent<GeneratedAssetMarker>().Initialize("AnotherPackage");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();

            Assert.Throws<InvalidOperationException>(() => new PrefabLayoutExporter().Export(CreateContext()));
            Assert.That(File.ReadAllText(LayoutPath), Is.EqualTo(layout));
        }

        [Test]
        public void ExportUsesSelectedPrefabSourceMetadata()
        {
            WriteSourcePackage(@"{
  ""root"": {
    ""type"": ""Panel"",
    ""id"": ""Root"",
    ""rect"": {
      ""anchorMin"": [0, 0],
      ""anchorMax"": [1, 1],
      ""position"": [0, 0],
      ""size"": [0, 0]
    },
    ""children"": [
      {
        ""type"": ""Panel"",
        ""id"": ""FreeNode"",
        ""rect"": {
          ""anchorMin"": [0, 0],
          ""anchorMax"": [0, 0],
          ""position"": [0, 0],
          ""size"": [100, 40]
        }
      }
    ]
  }
}");

            var root = new GameObject($"{PackageId}View", typeof(RectTransform));
            try
            {
                root.AddComponent<GeneratedAssetMarker>().Initialize(PackageId, PackageManifestPath);
                var freeNode = CreateRect("FreeNode", root.transform);
                freeNode.anchoredPosition = new Vector2(45f, -12f);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            var result = new KKUIPipeline().ExportPrefab(PrefabPath);

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(File.ReadAllText(LayoutPath), Does.Contain("\"position\": [45, -12]"));
        }

        private static void WriteSourcePackage(string layout)
        {
            File.WriteAllText(PackageManifestPath, $@"{{
  ""schemaVersion"": ""1.0"",
  ""packageId"": ""{PackageId}"",
  ""namespace"": ""KK.UI.UMG.Editor.Tests.Export"",
  ""version"": ""1.0.0"",
  ""designResolution"": {{ ""width"": 800, ""height"": 600 }},
  ""manifests"": {{
    ""layout"": ""layout.json"",
    ""assets"": ""assets.json"",
    ""bindings"": ""bindings.json"",
    ""codegen"": ""codegen.json"",
    ""strings"": ""strings.json""
  }},
  ""v1"": {{ ""controls"": [""Panel""] }}
}}");
            File.WriteAllText(LayoutPath, layout);
            File.WriteAllText($"{SourceRoot}/assets.json", "{\"assets\":[]}");
            File.WriteAllText($"{SourceRoot}/bindings.json", "{\"mvvm\":{\"fields\":[]},\"bindings\":[],\"events\":[]}");
            File.WriteAllText($"{SourceRoot}/codegen.json", $@"{{
  ""schemaVersion"": ""1.0"",
  ""namespace"": ""KK.UI.UMG.Editor.Tests.Export"",
  ""outputRoot"": ""../../Generated/{PackageId}"",
  ""addressablesKey"": ""UI/{PackageId}/{PackageId}View"",
  ""view"": {{ ""className"": ""{PackageId}View"", ""baseClass"": ""UIViewBase"" }},
  ""controller"": {{ ""className"": ""{PackageId}Controller"", ""baseClass"": ""UIControllerBase"" }},
  ""viewModel"": {{ ""className"": ""{PackageId}ViewModel"" }},
  ""requiredServices"": []
}}");
            File.WriteAllText($"{SourceRoot}/strings.json", "{\"schemaVersion\":\"1.0\",\"defaultCulture\":\"en\",\"cultures\":[\"en\"],\"strings\":{}}");
            File.WriteAllText($"{SourceRoot}/README.md", "# Export test package\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            new ValidationLedgerWriter().EnsureScaffold(KKUIPipelineContext.Load(PackageManifestPath, GeneratedParent));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static KKUIPipelineContext CreateContext()
        {
            return new KKUIPipelineContext
            {
                SourceRoot = Path.GetFullPath(SourceRoot),
                GeneratedRoot = Path.GetFullPath(GeneratedRoot),
                Package = new UiPackageManifest
                {
                    PackageId = PackageId,
                    Manifests = new UiManifestRefs { Layout = "layout.json" }
                }
            };
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }
    }
}
