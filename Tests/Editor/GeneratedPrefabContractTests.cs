using NUnit.Framework;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class GeneratedPrefabContractTests
    {
        private const string PrefabPath = "Assets/UI/Generated/SimpleMessageBox/Prefabs/SimpleMessageBoxView.prefab";

        [Test]
        public void SimpleMessageBoxPrefabHasV03LayerComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null, $"Run Generate before this test. Missing prefab: {PrefabPath}");
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<GraphicRaycaster>(), Is.Not.Null);
        }

        [Test]
        public void VerifierDoesNotRequireSerializedRootCanvas()
        {
            var verifierPath = "Packages/com.kk.ui-umg/Editor/Pipeline/GeneratedAssetVerifier.cs";
            var verifierSource = File.ReadAllText(verifierPath);

            Assert.That(verifierSource, Does.Not.Contain("VER016"));
            Assert.That(verifierSource, Does.Not.Contain("GetComponent<Canvas>()"));
            Assert.That(verifierSource, Does.Not.Contain("root must have Canvas."));
        }

        [Test]
        public void VerifierRejectsHandwrittenScriptsInsideGeneratedScriptsFolder()
        {
            var generatedRoot = Path.GetFullPath("Temp/KKUI/GeneratedOwnershipTest");
            var scriptsRoot = Path.Combine(generatedRoot, "Scripts");
            Directory.CreateDirectory(scriptsRoot);
            File.WriteAllText(Path.Combine(scriptsRoot, "ManualView.cs"), "public class ManualView {}");
            try
            {
                var context = new KKUIPipelineContext
                {
                    GeneratedRoot = generatedRoot,
                    Package = new UiPackageManifest { PackageId = "GeneratedOwnershipTest" }
                };
                var method = typeof(GeneratedAssetVerifier).GetMethod(
                    "VerifyGeneratedScriptOwnership",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.That(method, Is.Not.Null);
                method.Invoke(null, new object[] { context });

                Assert.That(context.Issues, Has.Some.Matches<KKUIPipelineIssue>(issue => issue.Code == "VER036"));
            }
            finally
            {
                if (Directory.Exists(generatedRoot))
                {
                    Directory.Delete(generatedRoot, true);
                }
            }
        }
    }
}
