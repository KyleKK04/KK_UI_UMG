using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KK.UI.UMG;

namespace KK.UI.UMG.Tests
{
    public sealed class LayerStackTests
    {
        [Test]
        public void PushOnlyChangesTopology()
        {
            var first = CreateView("first");
            var second = CreateView("second");
            try
            {
                var stack = new LayerStack();

                stack.Push("first", first);
                stack.Push("second", second);

                Assert.That(stack.Top.systemId, Is.EqualTo("second"));
                Assert.That(first.GetComponent<CanvasGroup>().interactable, Is.True);
                Assert.That(first.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
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
        public void PopOnlyChangesTopology()
        {
            var first = CreateView("first");
            var second = CreateView("second");
            try
            {
                var stack = new LayerStack();
                stack.Push("first", first);
                stack.Push("second", second);
                first.GetComponent<CanvasGroup>().interactable = false;
                first.GetComponent<CanvasGroup>().blocksRaycasts = false;

                stack.Pop("second");

                Assert.That(stack.Top.systemId, Is.EqualTo("first"));
                Assert.That(first.GetComponent<CanvasGroup>().interactable, Is.False);
                Assert.That(first.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
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

        [Test]
        public void StackSnapshotIsReusedUntilStackChanges()
        {
            var first = CreateView("first");
            var second = CreateView("second");
            try
            {
                var stack = new LayerStack();
                stack.Push("first", first);

                var firstSnapshot = stack.Stack;
                var secondSnapshot = stack.Stack;

                Assert.That(ReferenceEquals(firstSnapshot, secondSnapshot), Is.True);

                stack.Push("second", second);
                var changedSnapshot = stack.Stack;

                Assert.That(ReferenceEquals(firstSnapshot, changedSnapshot), Is.True);
                Assert.That(changedSnapshot, Is.EqualTo(new[] { "first", "second" }));
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }

        [Test]
        public void RemoveCanDeleteMiddleLayerWithoutChangingTop()
        {
            var first = CreateView("first");
            var second = CreateView("second");
            var third = CreateView("third");
            try
            {
                var stack = new LayerStack();
                stack.Push("first", first);
                stack.Push("second", second);
                stack.Push("third", third);

                Assert.That(stack.Remove("second"), Is.True);

                Assert.That(stack.Top.systemId, Is.EqualTo("third"));
                Assert.That(stack.Stack, Is.EqualTo(new[] { "first", "third" }));
                Assert.That(stack.IsTop("third"), Is.True);
                Assert.That(stack.IsTop("first"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
                Object.DestroyImmediate(third.gameObject);
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
