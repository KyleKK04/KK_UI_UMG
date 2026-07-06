using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KK.UI.UMG;

namespace KK.UI.UMG.Tests
{
    public sealed class LayerStackTests
    {
        [Test]
        public void PushDisablesPreviousTopAndActivatesNewTop()
        {
            var first = CreateView("first");
            var second = CreateView("second");
            try
            {
                var stack = new LayerStack();

                stack.Push("first", first);
                stack.Push("second", second);

                Assert.That(stack.Top.systemId, Is.EqualTo("second"));
                Assert.That(first.GetComponent<CanvasGroup>().interactable, Is.False);
                Assert.That(first.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(second.GetComponent<CanvasGroup>().interactable, Is.True);
                Assert.That(second.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }

        [Test]
        public void PopRestoresPreviousLayer()
        {
            var first = CreateView("first");
            var second = CreateView("second");
            try
            {
                var stack = new LayerStack();
                stack.Push("first", first);
                stack.Push("second", second);

                stack.Pop("second");

                Assert.That(stack.Top.systemId, Is.EqualTo("first"));
                Assert.That(first.GetComponent<CanvasGroup>().interactable, Is.True);
                Assert.That(first.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }

        [Test]
        public void PopNonTopLogsWarningAndKeepsStack()
        {
            var first = CreateView("first");
            var second = CreateView("second");
            try
            {
                var stack = new LayerStack();
                stack.Push("first", first);
                stack.Push("second", second);

                LogAssert.Expect(LogType.Warning, "[LayerStack] Pop('first') ignored: top layer is 'second'.");
                stack.Pop("first");

                Assert.That(stack.Top.systemId, Is.EqualTo("second"));
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }

        private static TestView CreateView(string name)
        {
            var gameObject = new GameObject(name);
            gameObject.AddComponent<CanvasGroup>();
            return gameObject.AddComponent<TestView>();
        }

        private sealed class TestView : UIViewBase
        {
            protected override void BindEvents()
            {
            }

            protected override void UnbindEvents()
            {
            }
        }
    }
}
