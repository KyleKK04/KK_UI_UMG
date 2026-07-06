using NUnit.Framework;
using UnityEngine;
using KK.UI.UMG;

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
            Object.DestroyImmediate(_managerObject);
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
}
