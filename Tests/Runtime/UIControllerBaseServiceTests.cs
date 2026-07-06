using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using KK.UI.UMG;

namespace KK.UI.UMG.Tests
{
    public sealed class UIControllerBaseServiceTests
    {
        private GameObject _managerObject;
        private UIManager _manager;

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("UIControllerBaseServiceTests");
            _managerObject.SetActive(false);
            _manager = _managerObject.AddComponent<UIManager>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_managerObject);
        }

        [Test]
        public void RequireServiceReturnsRegisteredService()
        {
            var service = new TestService();
            var controller = CreateController();
            _manager.RegisterService<ITestService>(service);

            Assert.That(controller.Resolve<ITestService>(), Is.SameAs(service));
        }

        [Test]
        public void RequireServiceMissingThrows()
        {
            var controller = CreateController();

            Assert.Throws<InvalidOperationException>(() => controller.Resolve<ITestService>());
        }

        [Test]
        public void TryGetServiceReturnsFalseWhenMissing()
        {
            var controller = CreateController();

            Assert.That(controller.TryResolve<ITestService>(out _), Is.False);
        }

        [Test]
        public void TrackSubscriptionDisposesOnce()
        {
            var controller = CreateController();
            var disposeCount = 0;

            controller.Track(UISubscription.Create(() => disposeCount++));
            controller.Dispose();
            controller.Dispose();

            Assert.That(disposeCount, Is.EqualTo(1));
        }

        [Test]
        public void TrackedSubscriptionStopsBusinessNotifications()
        {
            var controller = CreateController();
            var source = new BusinessSource();
            controller.TrackBusinessSource(source);

            source.RaiseChanged();
            Assert.That(controller.NotificationCount, Is.EqualTo(1));

            controller.Dispose();

            Assert.DoesNotThrow(() => source.RaiseChanged());
            Assert.That(controller.NotificationCount, Is.EqualTo(1));
        }

        private TestController CreateController()
        {
            var controller = new TestController();
            typeof(UIControllerBase)
                .GetProperty(nameof(UIControllerBase.UIManager), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(controller, _manager);
            return controller;
        }

        private interface ITestService
        {
        }

        private sealed class TestService : ITestService
        {
        }

        private sealed class BusinessSource
        {
            public event Action Changed;

            public void RaiseChanged()
            {
                Changed?.Invoke();
            }
        }

        private sealed class TestController : UIControllerBase
        {
            public int NotificationCount { get; private set; }

            public T Resolve<T>() where T : class
            {
                return RequireService<T>();
            }

            public bool TryResolve<T>(out T service) where T : class
            {
                return TryGetService(out service);
            }

            public IDisposable Track(IDisposable subscription)
            {
                return TrackSubscription(subscription);
            }

            public void TrackBusinessSource(BusinessSource source)
            {
                source.Changed += HandleChanged;
                TrackSubscription(UISubscription.Create(() => source.Changed -= HandleChanged));
            }

            private void HandleChanged()
            {
                NotificationCount++;
                Store.Update("NotificationCount", NotificationCount);
            }
        }
    }
}
