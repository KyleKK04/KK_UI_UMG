using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class SourcePackageValidatorTests
    {
        private string _sourceRoot;
        private readonly List<string> _sourceRoots = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _sourceRoots.Clear();
            _sourceRoot = Path.GetFullPath("Assets/UI/Source/SourceValidatorTest");
            PrepareSourceRoot(_sourceRoot);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var sourceRoot in _sourceRoots.Distinct())
            {
                if (Directory.Exists(sourceRoot))
                {
                    Directory.Delete(sourceRoot, true);
                }
            }
        }

        [Test]
        public void CustomAssetsSourceRootIsAllowed()
        {
            var customSourceRoot = Path.GetFullPath("Assets/_Project/UISource/SourceValidatorTest");
            PrepareSourceRoot(customSourceRoot);
            var context = CreateContext(customSourceRoot);

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC001"), Is.False);
        }

        [Test]
        public void SourceRootFolderMustMatchPackageId()
        {
            var context = CreateContext();
            context.SourceRoot = Path.GetFullPath("Assets/UI/Source/WrongPackage");

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC001"), Is.True);
        }

        [Test]
        public void SourceRootMustStayUnderAssetsOrPackages()
        {
            var context = CreateContext(Path.Combine(Path.GetTempPath(), "SourceValidatorTest"));

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC001"), Is.True);
        }

        [Test]
        public void SourceRootMustNotBeInsideGeneratedFolder()
        {
            var generatedSourceRoot = Path.GetFullPath("Assets/UI/Generated/SourceValidatorTest");
            PrepareSourceRoot(generatedSourceRoot);
            var context = CreateContext(generatedSourceRoot);

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC001"), Is.True);
        }

        [Test]
        public void ManifestReferencesUseV051FileNames()
        {
            var context = CreateContext();
            context.Package.Manifests.Strings = "strings.zh-Hans.json";

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC003"), Is.True);
        }

        [Test]
        public void ManifestReferencesMustStayInsideSourceRoot()
        {
            var context = CreateContext();
            context.Package.Manifests.Layout = "../layout.json";

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC004"), Is.True);
        }

        [Test]
        public void LegacyStringFileFails()
        {
            File.WriteAllText(Path.Combine(_sourceRoot, "strings.zh-Hans.json"), "{}");
            var context = CreateContext();

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC005"), Is.True);
        }

        [Test]
        public void GeneratedArtifactsInsideSourceFail()
        {
            var scriptsRoot = Path.Combine(_sourceRoot, "Scripts");
            Directory.CreateDirectory(scriptsRoot);
            File.WriteAllText(Path.Combine(scriptsRoot, "Bad.Generated.cs"), string.Empty);
            var context = CreateContext();

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC006"), Is.True);
        }

        [Test]
        public void SourcePackageValidationDocumentIsAllowed()
        {
            var context = CreateContext();

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC006"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "SRC009"), Is.False);
        }

        [Test]
        public void GeneratedReportsInsideSourceFail()
        {
            var reportsRoot = Path.Combine(_sourceRoot, "Reports");
            Directory.CreateDirectory(reportsRoot);
            File.WriteAllText(Path.Combine(reportsRoot, "validation.md"), string.Empty);
            var context = CreateContext();

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC006"), Is.True);
        }

        [Test]
        public void UndeclaredSourceAssetWarns()
        {
            var imagesRoot = Path.Combine(_sourceRoot, "Assets", "Images");
            Directory.CreateDirectory(imagesRoot);
            File.WriteAllText(Path.Combine(imagesRoot, "orphan.png"), string.Empty);
            var context = CreateContext();

            new SourcePackageValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "SRC007" && issue.Severity == KKUIPipelineIssueSeverity.Warning), Is.True);
        }

        private void PrepareSourceRoot(string sourceRoot)
        {
            if (Directory.Exists(sourceRoot))
            {
                Directory.Delete(sourceRoot, true);
            }

            Directory.CreateDirectory(sourceRoot);
            _sourceRoots.Add(sourceRoot);
            File.WriteAllText(Path.Combine(sourceRoot, "README.md"), "# SourceValidatorTest");
            new ValidationLedgerWriter().EnsureScaffold(CreateContext(sourceRoot));
        }

        private KKUIPipelineContext CreateContext(string sourceRoot = null)
        {
            sourceRoot = sourceRoot ?? _sourceRoot;
            return new KKUIPipelineContext
            {
                PackageManifestPath = Path.Combine(sourceRoot, "package.json"),
                SourceRoot = sourceRoot,
                GeneratedRoot = Path.GetFullPath("Assets/UI/Generated/SourceValidatorTest"),
                Package = new UiPackageManifest
                {
                    PackageId = "SourceValidatorTest",
                    Namespace = "Game.UI.SourceValidatorTest",
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
    }
}
