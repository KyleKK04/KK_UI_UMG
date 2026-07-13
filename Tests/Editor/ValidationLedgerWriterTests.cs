using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class ValidationLedgerWriterTests
    {
        private string _sourceRoot;

        [SetUp]
        public void SetUp()
        {
            _sourceRoot = Path.GetFullPath("Assets/UI/Source/LedgerWriterTest");
            DeleteIfExists(_sourceRoot);
            Directory.CreateDirectory(_sourceRoot);
            File.WriteAllText(Path.Combine(_sourceRoot, "README.md"), "# LedgerWriterTest");
        }

        [TearDown]
        public void TearDown()
        {
            DeleteIfExists(_sourceRoot);
        }

        [Test]
        public void MissingReadmeFails()
        {
            File.Delete(Path.Combine(_sourceRoot, "README.md"));
            var context = CreateContext();
            new ValidationLedgerWriter().EnsureScaffold(context);

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC008"), Is.True);
        }

        [Test]
        public void MissingValidationLedgerIsScaffolded()
        {
            var context = CreateContext();

            new ValidationLedgerWriter().EnsureScaffold(context);

            Assert.That(File.Exists(Path.Combine(_sourceRoot, "validation.md")), Is.True);
            new SourcePackageValidator().Validate(context);
            Assert.That(context.Issues.Any(issue => issue.Code == "SRC009"), Is.False);
        }

        [Test]
        public void InvalidValidationStatusFails()
        {
            var context = CreateContext();
            new ValidationLedgerWriter().EnsureScaffold(context);
            var path = Path.Combine(_sourceRoot, "validation.md");
            File.WriteAllText(path, File.ReadAllText(path).Replace("| Validate | NotRun |", "| Validate | OK |"));

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC009"), Is.True);
        }

        [Test]
        public void InvalidRuntimeStatusStillFails()
        {
            var context = CreateContext();
            new ValidationLedgerWriter().EnsureScaffold(context);
            var path = Path.Combine(_sourceRoot, "validation.md");
            File.WriteAllText(path, File.ReadAllText(path).Replace("| Runtime | Pending |", "| Runtime | OK |"));

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC009"), Is.True);
        }

        [Test]
        public void LegacyRuntimePassIsAcceptedAndRewrittenAsVerified()
        {
            var context = CreateContext();
            new ValidationLedgerWriter().EnsureScaffold(context);
            var path = Path.Combine(_sourceRoot, "validation.md");
            File.WriteAllText(path, File.ReadAllText(path).Replace(
                "| Runtime | Pending | - | Manual | Runtime behavior not verified |",
                "| Runtime | Pass | 2026-07-10T02:36:26Z | Manual PlayMode | Enter flow verified |"));

            new SourcePackageValidator().Validate(context);
            Assert.That(context.Issues.Any(issue => issue.Code == "SRC009"), Is.False);

            new ValidationLedgerWriter().WritePipelineResult(context, new GamePipelineResultBuilder("Validate", true).Build());
            var text = File.ReadAllText(path);
            Assert.That(text, Does.Contain("| Runtime | Verified | 2026-07-10T02:36:26Z | Manual PlayMode | Enter flow verified |"));
            Assert.That(text, Does.Not.Contain("| Runtime | Pass |"));
        }

        [Test]
        public void ValidationLedgerPreservesManualNotes()
        {
            var context = CreateContext();
            var path = Path.Combine(_sourceRoot, "validation.md");
            File.WriteAllText(path, "# LedgerWriterTest Validation\n\nManual intro.\n");

            new ValidationLedgerWriter().WritePipelineResult(context, new GamePipelineResultBuilder("Validate", true).Build());

            var text = File.ReadAllText(path);
            Assert.That(text, Does.Contain("Manual intro."));
            Assert.That(text, Does.Contain("| Validate | Pass |"));
        }

        [Test]
        public void RuntimeVerifiedIsPreserved()
        {
            var context = CreateContext();
            new ValidationLedgerWriter().EnsureScaffold(context);
            var path = Path.Combine(_sourceRoot, "validation.md");
            File.WriteAllText(path, File.ReadAllText(path).Replace("| Runtime | Pending |", "| Runtime | Verified |"));

            new ValidationLedgerWriter().WritePipelineResult(context, new GamePipelineResultBuilder("Verify", true).Build());

            Assert.That(File.ReadAllText(path), Does.Contain("| Runtime | Verified |"));
        }

        [Test]
        public void RuntimeStatusCanBeExplicitlyVerifiedAndReset()
        {
            var context = CreateContext();
            var writer = new ValidationLedgerWriter();
            writer.WritePipelineResult(context, new GamePipelineResultBuilder("Generate", true).Build());

            writer.WriteRuntimeStatus(context, true, "Manual PlayMode", "Enter button loaded AsylumDemo");
            var path = Path.Combine(_sourceRoot, "validation.md");
            var verified = File.ReadAllText(path);
            Assert.That(verified, Does.Contain("| Runtime | Verified |"));
            Assert.That(verified, Does.Contain("| Manual PlayMode | Enter button loaded AsylumDemo |"));

            writer.WriteRuntimeStatus(context, false, "Manual", "Runtime behavior requires re-verification.");
            var pending = File.ReadAllText(path);
            Assert.That(pending, Does.Contain("| Runtime | Pending |"));
        }

        [Test]
        public void RuntimeCannotBeVerifiedBeforeStaticPipelinePasses()
        {
            var context = CreateContext();
            var writer = new ValidationLedgerWriter();
            writer.EnsureScaffold(context);

            Assert.Throws<System.InvalidOperationException>(() =>
                writer.WriteRuntimeStatus(context, true, "Manual PlayMode", "Checked"));
        }

        [Test]
        public void PreviewUpdatesLedgerWithoutScreenshot()
        {
            var context = CreateContext();

            new ValidationLedgerWriter().WritePreviewResult(context, true, "left | right");

            var text = File.ReadAllText(Path.Combine(_sourceRoot, "validation.md"));
            Assert.That(text, Does.Contain("| Preview | Pass |"));
            Assert.That(text, Does.Contain("left / right"));
            Assert.That(Directory.GetFiles(_sourceRoot, "*.png", SearchOption.AllDirectories), Is.Empty);
        }

        [Test]
        public void GeneratePassIsPreservedWhenVerifyFails()
        {
            var context = CreateContext();
            var result = new KKUIPipelineResult
            {
                Operation = "Generate",
                Status = "Failed",
                Success = false,
                Issues = new List<KKUIPipelineIssue>
                {
                    new KKUIPipelineIssue
                    {
                        Severity = KKUIPipelineIssueSeverity.Error,
                        Code = "VER001",
                        Message = "verify failed"
                    }
                }
            };

            new ValidationLedgerWriter().WritePipelineResult(context, result);

            var text = File.ReadAllText(Path.Combine(_sourceRoot, "validation.md"));
            Assert.That(text, Does.Contain("| Generate | Pass |"));
            Assert.That(text, Does.Contain("| Verify | Fail |"));
        }

        private KKUIPipelineContext CreateContext()
        {
            return new KKUIPipelineContext
            {
                PackageManifestPath = Path.Combine(_sourceRoot, "package.json"),
                SourceRoot = _sourceRoot,
                GeneratedRoot = Path.GetFullPath("Assets/UI/Generated/LedgerWriterTest"),
                Package = new UiPackageManifest
                {
                    PackageId = "LedgerWriterTest",
                    Namespace = "Game.UI.LedgerWriterTest",
                    Manifests = new UiManifestRefs
                    {
                        Layout = "layout.json",
                        Assets = "assets.json",
                        Bindings = "bindings.json",
                        Codegen = "codegen.json",
                        Strings = "strings.json"
                    }
                },
                Assets = new UiAssetsManifest
                {
                    Assets = new List<UiAssetSpec>()
                }
            };
        }

        private static void DeleteIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            var meta = path + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
        }

        private sealed class GamePipelineResultBuilder
        {
            private readonly string _operation;
            private readonly bool _success;

            public GamePipelineResultBuilder(string operation, bool success)
            {
                _operation = operation;
                _success = success;
            }

            public KKUIPipelineResult Build()
            {
                return new KKUIPipelineResult
                {
                    Operation = _operation,
                    Status = _success ? "Succeeded" : "Failed",
                    Success = _success,
                    Issues = new List<KKUIPipelineIssue>()
                };
            }
        }
    }
}
