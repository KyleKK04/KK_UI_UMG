using NUnit.Framework;
using UnityEngine;
using KK.UI.UMG;

namespace KK.UI.UMG.Tests
{
    public sealed class UIControllerBaseTests
    {
        [Test]
        public void BindViewStoresControllerAndViewReferences()
        {
            var viewObject = new GameObject("view");
            try
            {
                var view = viewObject.AddComponent<TestView>();
                var controller = new TestController();

                controller.BindView(view);

                Assert.That(controller.View, Is.SameAs(view));
                Assert.That(view.Controller, Is.SameAs(controller));
                Assert.That(controller.ViewBoundCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
        }

        [Test]
        public void BindViewCanOnlySucceedOnce()
        {
            var firstObject = new GameObject("first");
            var secondObject = new GameObject("second");
            try
            {
                var controller = new TestController();
                controller.BindView(firstObject.AddComponent<TestView>());

                Assert.Throws<System.InvalidOperationException>(() => controller.BindView(secondObject.AddComponent<TestView>()));
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void DisposeIsNoOpAfterFirstCallAndDisposesStore()
        {
            var controller = new TestController();
            controller.Store.Update("Message", "hello");

            controller.Dispose();
            controller.Dispose();

            Assert.That(controller.DisposeCount, Is.EqualTo(1));
            Assert.Throws<System.ObjectDisposedException>(() => controller.Store.Update("Message", "goodbye"));
        }

        private sealed class TestController : UIControllerBase
        {
            public int ViewBoundCount { get; private set; }
            public int DisposeCount { get; private set; }

            protected override void OnViewBound(UIViewBase view)
            {
                ViewBoundCount++;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                DisposeCount++;
            }
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
