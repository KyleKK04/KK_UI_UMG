using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
    }
}
