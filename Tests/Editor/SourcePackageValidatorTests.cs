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

        [SetUp]
        public void SetUp()
        {
            _sourceRoot = Path.GetFullPath("Assets/UI/Source/SourceValidatorTest");
            if (Directory.Exists(_sourceRoot))
            {
                Directory.Delete(_sourceRoot, true);
            }

            Directory.CreateDirectory(_sourceRoot);
            File.WriteAllText(Path.Combine(_sourceRoot, "README.md"), "# SourceValidatorTest");
            new ValidationLedgerWriter().EnsureScaffold(CreateContext());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_sourceRoot))
            {
                Directory.Delete(_sourceRoot, true);
            }
        }

        [Test]
        public void SourceRootMustMatchPackageId()
        {
            var context = CreateContext();
            context.SourceRoot = Path.GetFullPath("Assets/UI/Source/WrongPackage");

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

        private KKUIPipelineContext CreateContext()
        {
            return new KKUIPipelineContext
            {
                PackageManifestPath = Path.Combine(_sourceRoot, "package.json"),
                SourceRoot = _sourceRoot,
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
