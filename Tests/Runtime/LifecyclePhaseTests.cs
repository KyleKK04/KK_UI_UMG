using NUnit.Framework;
using KK.UI.UMG;

namespace KK.UI.UMG.Tests
{
    public sealed class LifecyclePhaseTests
    {
        [Test]
        public void LifecyclePhaseOrderMatchesV03Contract()
        {
            Assert.That((int)LifecyclePhase.PreOpen, Is.LessThan((int)LifecyclePhase.Opened));
            Assert.That((int)LifecyclePhase.Opened, Is.LessThan((int)LifecyclePhase.Activated));
            Assert.That((int)LifecyclePhase.Activated, Is.LessThan((int)LifecyclePhase.Deactivated));
            Assert.That((int)LifecyclePhase.Deactivated, Is.LessThan((int)LifecyclePhase.PreClose));
            Assert.That((int)LifecyclePhase.PreClose, Is.LessThan((int)LifecyclePhase.Closed));
        }
    }
}
