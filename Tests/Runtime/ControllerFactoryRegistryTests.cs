using System;
using System.Collections.Generic;
using NUnit.Framework;
using KK.UI.UMG;
using KK.UI.UMG.Internal;

namespace KK.UI.UMG.Tests
{
    public sealed class ControllerFactoryRegistryTests
    {
        [Test]
        public void CopyToCopiesRegisteredFactory()
        {
            var systemId = "RegistryTest_" + Guid.NewGuid().ToString("N");
            ControllerFactoryRegistry.Register(systemId, () => new TestController());
            var target = new Dictionary<string, Func<UIControllerBase>>();

            ControllerFactoryRegistry.CopyTo(target);

            Assert.That(target, Contains.Key(systemId));
            Assert.That(target[systemId](), Is.TypeOf<TestController>());
        }

        [Test]
        public void CopyToClearsTargetBeforeCopying()
        {
            var target = new Dictionary<string, Func<UIControllerBase>>
            {
                { "stale", () => new TestController() }
            };

            ControllerFactoryRegistry.CopyTo(target);

            Assert.That(target.ContainsKey("stale"), Is.False);
        }

        private sealed class TestController : UIControllerBase
        {
        }
    }
}
