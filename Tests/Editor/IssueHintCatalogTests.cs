using KK.UI.UMG.Editor.Pipeline;
using NUnit.Framework;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class IssueHintCatalogTests
    {
        [Test]
        public void CommonIssueCodesHaveFixHints()
        {
            Assert.That(IssueHintCatalog.GetHint("SRC001"), Does.Contain("Source package"));
            Assert.That(IssueHintCatalog.GetHint("SRC009"), Does.Contain("Runtime uses Pending or Verified"));
            Assert.That(IssueHintCatalog.GetHint("TXT003"), Does.Contain("dynamic text binding"));
            Assert.That(IssueHintCatalog.GetHint("GEN006"), Does.Contain("UI/<PackageId>/<PackageId>View"));
            Assert.That(IssueHintCatalog.GetHint("GENPENDING"), Does.Contain("compilation"));
            Assert.That(IssueHintCatalog.GetHint("VER036"), Does.Contain("handwritten"));
        }

        [Test]
        public void UnknownIssueCodeHasNoHint()
        {
            Assert.That(IssueHintCatalog.GetHint("UNKNOWN"), Is.Null);
        }
    }
}
