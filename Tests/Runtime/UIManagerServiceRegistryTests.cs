using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using KK.UI.UMG;
using KK.UI.UMG.Internal;

namespace KK.UI.UMG.Tests
{
    public sealed class UIManagerServiceRegistryTests
    {
        private GameObject _managerObject;
        private UIManager _manager;

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("UIManagerServiceRegistryTests");
            _managerObject.SetActive(false);
            _manager = _managerObject.AddComponent<UIManager>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_managerObject);
        }

        [Test]
        public void RegisterServiceStoresInstance()
        {
            var service = new TestService();

            _manager.RegisterService<ITestService>(service);

            Assert.That(_manager.TryGetService<ITestService>(out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(service));
        }

        [Test]
        public void RegisterServiceRejectsNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => _manager.RegisterService<ITestService>(null));
        }

        [Test]
        public void RegisterServiceRejectsDuplicate()
        {
            _manager.RegisterService<ITestService>(new TestService());

            Assert.Throws<System.InvalidOperationException>(() => _manager.RegisterService<ITestService>(new TestService()));
        }

        [Test]
        public void UnregisterServiceRemovesInstance()
        {
            _manager.RegisterService<ITestService>(new TestService());

            _manager.UnregisterService<ITestService>();

            Assert.That(_manager.TryGetService<ITestService>(out _), Is.False);
        }

        [Test]
        public void ClearServicesRemovesAll()
        {
            _manager.RegisterService<ITestService>(new TestService());
            _manager.RegisterService<IOtherService>(new OtherService());

            _manager.ClearServices();

            Assert.That(_manager.TryGetService<ITestService>(out _), Is.False);
            Assert.That(_manager.TryGetService<IOtherService>(out _), Is.False);
        }

        private interface ITestService
        {
        }

        private interface IOtherService
        {
        }

        private sealed class TestService : ITestService
        {
        }

        private sealed class OtherService : IOtherService
        {
        }
    }

    public sealed class UIManagerTransitionTests
    {
        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, TestController> _controllers = new Dictionary<string, TestController>();
        private readonly Dictionary<string, int> _releaseCounts = new Dictionary<string, int>();
        private GameObject _managerObject;
        private UIManager _manager;
        private string _firstId;
        private string _secondId;

        [SetUp]
        public void SetUp()
        {
            TestView.Transition = null;
            _firstId = "TransitionA_" + Guid.NewGuid().ToString("N");
            _secondId = "TransitionB_" + Guid.NewGuid().ToString("N");
            AddPrefab(_firstId);
            AddPrefab(_secondId);

            _managerObject = new GameObject("UIManagerTransitionTests");
            _managerObject.SetActive(false);
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(_managerObject.transform, false);
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(_managerObject.transform, false);

            _manager = _managerObject.AddComponent<UIManager>();
            _manager.PrefabLoaderOverride = systemId => Task.FromResult(_prefabs[systemId]);
            _manager.PrefabReleaseOverride = systemId => _releaseCounts[systemId] = ReleaseCount(systemId) + 1;
            _manager.ControllerFactoryOverride = systemId =>
            {
                var controller = new TestController();
                _controllers[systemId] = controller;
                return controller;
            };
            _manager.TransitionTimeoutSeconds = 0.05f;
            _managerObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            TestView.Transition = null;
            if (_managerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_managerObject);
            }

            foreach (var prefab in _prefabs.Values)
            {
                if (prefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(prefab);
                }
            }

            _prefabs.Clear();
            _controllers.Clear();
            _releaseCounts.Clear();
        }

        [UnityTest]
        public IEnumerator OpenWaitsForTransitionAndReusesDuplicateTask()
        {
            var openCompletion = NewCompletion();
            TestView.Transition = (systemId, phase, _) =>
                systemId == _firstId && phase == "Open" ? openCompletion.Task : Task.CompletedTask;

            var first = _manager.OpenAsync(_firstId);
            var duplicate = _manager.OpenAsync(_firstId);

            Assert.That(duplicate, Is.SameAs(first));
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Loading));
            Assert.That(_controllers[_firstId].ActivatedCount, Is.Zero);
            var canvasGroup = _controllers[_firstId].View.GetComponent<CanvasGroup>();
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);

            openCompletion.SetResult(true);
            yield return WaitForTask(first);
            AssertSucceeded(first);

            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Open));
            Assert.That(_controllers[_firstId].ActivatedCount, Is.EqualTo(1));
            Assert.That(canvasGroup.interactable, Is.True);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);
        }

        [UnityTest]
        public IEnumerator DifferentViewsTransitionConcurrentlyAndOnlyReadyTopActivates()
        {
            var firstOpen = NewCompletion();
            var secondOpen = NewCompletion();
            TestView.Transition = (systemId, phase, _) => phase != "Open"
                ? Task.CompletedTask
                : systemId == _firstId ? firstOpen.Task : secondOpen.Task;

            var firstTask = _manager.OpenAsync(_firstId);
            var secondTask = _manager.OpenAsync(_secondId);

            Assert.That(_manager.GetTopLayer(), Is.SameAs(_controllers[_secondId].View));
            firstOpen.SetResult(true);
            yield return WaitForTask(firstTask);
            AssertSucceeded(firstTask);

            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Open));
            Assert.That(_controllers[_firstId].ActivatedCount, Is.Zero);
            Assert.That(_controllers[_firstId].View.GetComponent<CanvasGroup>().interactable, Is.False);

            secondOpen.SetResult(true);
            yield return WaitForTask(secondTask);
            AssertSucceeded(secondTask);
            Assert.That(_controllers[_secondId].ActivatedCount, Is.EqualTo(1));

            var closeTask = _manager.CloseAsync(_secondId);
            yield return WaitForTask(closeTask);
            AssertSucceeded(closeTask);
            Assert.That(_manager.GetTopLayer(), Is.SameAs(_controllers[_firstId].View));
            Assert.That(_controllers[_firstId].ActivatedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HideCanFinishAsMiddleLayerWithoutChangingNewTop()
        {
            var firstOpen = _manager.OpenAsync(_firstId);
            yield return WaitForTask(firstOpen);
            AssertSucceeded(firstOpen);

            var hideCompletion = NewCompletion();
            TestView.Transition = (systemId, phase, _) =>
                systemId == _firstId && phase == "Hide" ? hideCompletion.Task : Task.CompletedTask;
            var hideTask = _manager.HideAsync(_firstId);
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Hiding));

            var secondOpen = _manager.OpenAsync(_secondId);
            yield return WaitForTask(secondOpen);
            AssertSucceeded(secondOpen);
            var secondView = _controllers[_secondId].View;

            hideCompletion.SetResult(true);
            yield return WaitForTask(hideTask);
            AssertSucceeded(hideTask);

            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Hidden));
            Assert.That(_manager.GetTopLayer(), Is.SameAs(secondView));
            Assert.That(_manager.GetLayerStack(), Is.EqualTo(new[] { _secondId }));
            Assert.That(_controllers[_secondId].ActivatedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OpenTimeoutRollsBackAndReleasesPanel()
        {
            var neverCompletes = NewCompletion();
            TestView.Transition = (systemId, phase, _) =>
                systemId == _firstId && phase == "Open" ? neverCompletes.Task : Task.CompletedTask;
            _manager.TransitionTimeoutSeconds = 0.01f;

            var task = _manager.OpenAsync(_firstId);
            yield return WaitForTask(task);

            AssertTimeout(task);
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Unloaded));
            Assert.That(_manager.GetLayerStack(), Is.Empty);
            Assert.That(_controllers[_firstId].DisposeCount, Is.EqualTo(1));
            Assert.That(ReleaseCount(_firstId), Is.EqualTo(1));
            neverCompletes.TrySetResult(true);
        }

        [UnityTest]
        public IEnumerator CloseTimeoutReturnsToOpenWithoutCommittingClose()
        {
            var openTask = _manager.OpenAsync(_firstId);
            yield return WaitForTask(openTask);
            AssertSucceeded(openTask);

            var neverCompletes = NewCompletion();
            TestView.Transition = (systemId, phase, _) =>
                systemId == _firstId && phase == "Close" ? neverCompletes.Task : Task.CompletedTask;
            _manager.TransitionTimeoutSeconds = 0.01f;

            var closeTask = _manager.CloseAsync(_firstId);
            yield return WaitForTask(closeTask);

            AssertTimeout(closeTask);
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Open));
            Assert.That(_controllers[_firstId].CloseCount, Is.Zero);
            Assert.That(_controllers[_firstId].ClosedCount, Is.Zero);
            Assert.That(_controllers[_firstId].DisposeCount, Is.Zero);
            Assert.That(_controllers[_firstId].ActivatedCount, Is.EqualTo(2));
            Assert.That(_controllers[_firstId].View.GetComponent<CanvasGroup>().interactable, Is.True);
            neverCompletes.TrySetResult(true);
        }

        [UnityTest]
        public IEnumerator HideTimeoutReturnsToOpenAndRestoresInteraction()
        {
            var openTask = _manager.OpenAsync(_firstId);
            yield return WaitForTask(openTask);
            AssertSucceeded(openTask);

            var neverCompletes = NewCompletion();
            TestView.Transition = (systemId, phase, _) =>
                systemId == _firstId && phase == "Hide" ? neverCompletes.Task : Task.CompletedTask;
            _manager.TransitionTimeoutSeconds = 0.01f;

            var hideTask = _manager.HideAsync(_firstId);
            yield return WaitForTask(hideTask);

            AssertTimeout(hideTask);
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Open));
            Assert.That(_manager.GetTopLayer(), Is.SameAs(_controllers[_firstId].View));
            Assert.That(_controllers[_firstId].ActivatedCount, Is.EqualTo(2));
            Assert.That(_controllers[_firstId].DeactivatedCount, Is.EqualTo(1));
            Assert.That(_controllers[_firstId].View.GetComponent<CanvasGroup>().interactable, Is.True);
            neverCompletes.TrySetResult(true);
        }

        [UnityTest]
        public IEnumerator ShowTimeoutReturnsToHiddenAndRemovesLayer()
        {
            var openTask = _manager.OpenAsync(_firstId);
            yield return WaitForTask(openTask);
            AssertSucceeded(openTask);
            var hideTask = _manager.HideAsync(_firstId);
            yield return WaitForTask(hideTask);
            AssertSucceeded(hideTask);

            var neverCompletes = NewCompletion();
            TestView.Transition = (systemId, phase, _) =>
                systemId == _firstId && phase == "Show" ? neverCompletes.Task : Task.CompletedTask;
            _manager.TransitionTimeoutSeconds = 0.01f;

            var showTask = _manager.ShowAsync(_firstId);
            yield return WaitForTask(showTask);

            AssertTimeout(showTask);
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Hidden));
            Assert.That(_manager.GetLayerStack(), Is.Empty);
            Assert.That(_controllers[_firstId].View.gameObject.activeSelf, Is.False);
            Assert.That(_controllers[_firstId].HiddenCount, Is.EqualTo(2));
            neverCompletes.TrySetResult(true);
        }

        [UnityTest]
        public IEnumerator TransitionExceptionLogsAndCommitsTargetState()
        {
            TestView.Transition = (systemId, phase, _) =>
                systemId == _firstId && phase == "Open"
                    ? Task.FromException(new InvalidOperationException("expected transition failure"))
                    : Task.CompletedTask;
            LogAssert.Expect(LogType.Error, new Regex("\\[UIManager\\].* Open transition .* failed and will continue"));

            var openTask = _manager.OpenAsync(_firstId);
            yield return WaitForTask(openTask);

            AssertSucceeded(openTask);
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Open));
            Assert.That(_controllers[_firstId].ActivatedCount, Is.EqualTo(1));
            Assert.That(_controllers[_firstId].View.GetComponent<CanvasGroup>().interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator OppositeCommandsForSameViewAreSerialized()
        {
            var phases = new List<string>();
            var openCompletion = NewCompletion();
            TestView.Transition = (systemId, phase, _) =>
            {
                if (systemId != _firstId)
                {
                    return Task.CompletedTask;
                }

                phases.Add(phase);
                return phase == "Open" ? openCompletion.Task : Task.CompletedTask;
            };

            var openTask = _manager.OpenAsync(_firstId);
            var closeTask = _manager.CloseAsync(_firstId);

            Assert.That(phases, Is.EqualTo(new[] { "Open" }));
            Assert.That(closeTask.IsCompleted, Is.False);
            openCompletion.SetResult(true);
            yield return WaitForTask(closeTask);

            AssertSucceeded(openTask);
            AssertSucceeded(closeTask);
            Assert.That(phases, Is.EqualTo(new[] { "Open", "Close" }));
            Assert.That(_manager.GetState(_firstId), Is.EqualTo(UIState.Unloaded));
            Assert.That(_controllers[_firstId].CloseCount, Is.EqualTo(1));
            Assert.That(_controllers[_firstId].DisposeCount, Is.EqualTo(1));
        }

        private void AddPrefab(string systemId)
        {
            var prefab = new GameObject(systemId + "View", typeof(RectTransform), typeof(CanvasGroup), typeof(TestView));
            prefab.SetActive(false);
            _prefabs.Add(systemId, prefab);
        }

        private int ReleaseCount(string systemId)
        {
            return _releaseCounts.TryGetValue(systemId, out var count) ? count : 0;
        }

        private static TaskCompletionSource<bool> NewCompletion()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private static IEnumerator WaitForTask(Task task)
        {
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (!task.IsCompleted && DateTime.UtcNow < deadline)
            {
                yield return null;
            }

            Assert.That(task.IsCompleted, Is.True, "Timed out waiting for UI task to complete.");
        }

        private static void AssertSucceeded(Task task)
        {
            Assert.That(task.IsCanceled, Is.False);
            Assert.That(task.Exception, Is.Null, task.Exception?.ToString());
        }

        private static void AssertTimeout(Task task)
        {
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.Flatten().InnerExceptions.Any(exception => exception is TimeoutException), Is.True, task.Exception.ToString());
        }

        private sealed class TestView : UIViewBase
        {
            public static Func<string, string, CancellationToken, Task> Transition { get; set; }

            protected override Task OnPlayOpenTransitionAsync(CancellationToken cancellationToken)
            {
                return Play("Open", cancellationToken);
            }

            protected override Task OnPlayShowTransitionAsync(CancellationToken cancellationToken)
            {
                return Play("Show", cancellationToken);
            }

            protected override Task OnPlayHideTransitionAsync(CancellationToken cancellationToken)
            {
                return Play("Hide", cancellationToken);
            }

            protected override Task OnPlayCloseTransitionAsync(CancellationToken cancellationToken)
            {
                return Play("Close", cancellationToken);
            }

            protected override void BindEvents()
            {
            }

            protected override void UnbindEvents()
            {
            }

            private Task Play(string phase, CancellationToken cancellationToken)
            {
                return Transition?.Invoke(Controller.SystemId, phase, cancellationToken) ?? Task.CompletedTask;
            }
        }

        private sealed class TestController : UIControllerBase
        {
            public int ActivatedCount { get; private set; }
            public int DeactivatedCount { get; private set; }
            public int HiddenCount { get; private set; }
            public int CloseCount { get; private set; }
            public int ClosedCount { get; private set; }
            public int DisposeCount { get; private set; }

            public override void OnActivated()
            {
                ActivatedCount++;
            }

            public override void OnDeactivated()
            {
                DeactivatedCount++;
            }

            public override void OnHidden()
            {
                HiddenCount++;
            }

            public override void Close()
            {
                CloseCount++;
            }

            public override void OnClosed()
            {
                ClosedCount++;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                DisposeCount++;
            }
        }
    }
}
