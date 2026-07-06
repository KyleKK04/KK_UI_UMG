using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class PrefabPreviewRendererTests
    {
        private const string PackageId = "PreviewRendererTest";
        private const string SourceRoot = "Assets/UI/Source/PreviewRendererTest";
        private const string GeneratedRoot = "Assets/UI/Generated/PreviewRendererTest";
        private const string PrefabPath = GeneratedRoot + "/Prefabs/PreviewRendererTestView.prefab";

        [SetUp]
        public void SetUp()
        {
            DeleteIfExists(SourceRoot);
            DeleteIfExists(GeneratedRoot);
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PreviewExplodingView.BindEventsCalled = false;
        }

        [TearDown]
        public void TearDown()
        {
            DeleteIfExists(SourceRoot);
            DeleteIfExists(GeneratedRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void RendersPreviewForGeneratedPrefab()
        {
            CreatePrefab(false);

            var result = new PrefabPreviewRenderer().Render(CreateContext(320, 180));

            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                Assert.That(result.Texture, Is.Not.Null);
                Assert.That(result.Texture.width, Is.EqualTo(320));
                Assert.That(result.Texture.height, Is.EqualTo(180));
                Assert.That(result.Texture.GetPixels32().Any(pixel => pixel.g > 80 || pixel.b > 80), Is.True);
            }
            finally
            {
                DestroyTexture(result);
            }
        }

        [Test]
        public void PreviewDoesNotRequireController()
        {
            CreatePrefab(false);

            var result = new PrefabPreviewRenderer().Render(CreateContext(320, 180));

            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                Assert.That(result.Texture, Is.Not.Null);
            }
            finally
            {
                DestroyTexture(result);
            }
        }

        [Test]
        public void PreviewDisablesGeneratedViewRuntimeLogic()
        {
            CreatePrefab(true);

            var result = new PrefabPreviewRenderer().Render(CreateContext(320, 180));

            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                Assert.That(PreviewExplodingView.BindEventsCalled, Is.False);
            }
            finally
            {
                DestroyTexture(result);
            }
        }

        [Test]
        public void PreviewReportsMissingPrefab()
        {
            var result = new PrefabPreviewRenderer().Render(CreateContext(320, 180));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(PrefabPreviewStatus.PreviewFailed));
            Assert.That(result.Error, Does.Contain("Generated prefab does not exist"));
            Assert.That(result.Texture, Is.Null);
        }

        [Test]
        public void PreviewReportsBackgroundOnlyRender()
        {
            CreateEmptyPrefab();

            var result = new PrefabPreviewRenderer().Render(CreateContext(320, 180));

            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(PrefabPreviewStatus.PreviewFailed));
            Assert.That(result.Error, Does.Contain("camera background"));
            Assert.That(result.Texture, Is.Null);
        }

        [Test]
        public void PreviewUsesDesignResolution()
        {
            CreatePrefab(false);

            var result = new PrefabPreviewRenderer().Render(CreateContext(256, 144));

            try
            {
                Assert.That(result.Success, Is.True, result.Error);
                Assert.That(result.Width, Is.EqualTo(256));
                Assert.That(result.Height, Is.EqualTo(144));
                Assert.That(result.Texture.width, Is.EqualTo(256));
                Assert.That(result.Texture.height, Is.EqualTo(144));
            }
            finally
            {
                DestroyTexture(result);
            }
        }

        private static KKUIPipelineContext CreateContext(int width, int height)
        {
            return new KKUIPipelineContext
            {
                SourceRoot = Path.GetFullPath(SourceRoot),
                GeneratedRoot = Path.GetFullPath(GeneratedRoot),
                Package = new UiPackageManifest
                {
                    PackageId = PackageId,
                    DesignResolution = new UiDesignResolution
                    {
                        Width = width,
                        Height = height
                    }
                }
            };
        }

        private static void CreatePrefab(bool addExplodingView)
        {
            var root = new GameObject("PreviewRendererTestView", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            root.SetActive(false);
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                root.GetComponent<Image>().color = new Color(0.1f, 0.55f, 0.9f, 1f);

                if (addExplodingView)
                {
                    root.AddComponent<PreviewExplodingView>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.ImportAsset(PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateEmptyPrefab()
        {
            var root = new GameObject("PreviewRendererTestView", typeof(RectTransform), typeof(CanvasGroup));
            root.SetActive(false);
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.ImportAsset(PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void DestroyTexture(PrefabPreviewResult result)
        {
            if (result?.Texture != null)
            {
                Object.DestroyImmediate(result.Texture);
            }
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

    public sealed class PreviewExplodingView : UIViewBase
    {
        public static bool BindEventsCalled { get; set; }

        protected override void BindEvents()
        {
            BindEventsCalled = true;
            throw new System.InvalidOperationException("Preview must not enable generated view logic.");
        }

        protected override void UnbindEvents()
        {
        }
    }
}
