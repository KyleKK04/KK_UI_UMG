using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        [Test]
        public void DefaultViewTransitionsCompleteImmediately()
        {
            var viewObject = new GameObject("view");
            try
            {
                var view = viewObject.AddComponent<TestView>();

                Assert.That(view.PlayOpenTransitionAsync(CancellationToken.None).IsCompletedSuccessfully, Is.True);
                Assert.That(view.PlayShowTransitionAsync(CancellationToken.None).IsCompletedSuccessfully, Is.True);
                Assert.That(view.PlayHideTransitionAsync(CancellationToken.None).IsCompletedSuccessfully, Is.True);
                Assert.That(view.PlayCloseTransitionAsync(CancellationToken.None).IsCompletedSuccessfully, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
        }

        [Test]
        public void ViewTransitionWrappersForwardEachPhaseAndTokenOnce()
        {
            var viewObject = new GameObject("view");
            using var cancellation = new CancellationTokenSource();
            try
            {
                var view = viewObject.AddComponent<TransitionView>();

                view.PlayOpenTransitionAsync(cancellation.Token).GetAwaiter().GetResult();
                view.PlayShowTransitionAsync(cancellation.Token).GetAwaiter().GetResult();
                view.PlayHideTransitionAsync(cancellation.Token).GetAwaiter().GetResult();
                view.PlayCloseTransitionAsync(cancellation.Token).GetAwaiter().GetResult();

                Assert.That(view.Phases, Is.EqualTo(new[] { "Open", "Show", "Hide", "Close" }));
                Assert.That(view.Tokens, Has.All.EqualTo(cancellation.Token));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
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

        private sealed class TransitionView : UIViewBase
        {
            public List<string> Phases { get; } = new List<string>();
            public List<CancellationToken> Tokens { get; } = new List<CancellationToken>();

            protected override Task OnPlayOpenTransitionAsync(CancellationToken cancellationToken)
            {
                return Record("Open", cancellationToken);
            }

            protected override Task OnPlayShowTransitionAsync(CancellationToken cancellationToken)
            {
                return Record("Show", cancellationToken);
            }

            protected override Task OnPlayHideTransitionAsync(CancellationToken cancellationToken)
            {
                return Record("Hide", cancellationToken);
            }

            protected override Task OnPlayCloseTransitionAsync(CancellationToken cancellationToken)
            {
                return Record("Close", cancellationToken);
            }

            protected override void BindEvents()
            {
            }

            protected override void UnbindEvents()
            {
            }

            private Task Record(string phase, CancellationToken cancellationToken)
            {
                Phases.Add(phase);
                Tokens.Add(cancellationToken);
                return Task.CompletedTask;
            }
        }
    }
}
