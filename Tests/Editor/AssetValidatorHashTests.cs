using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;
using KK.UI.UMG.Editor.Validators;
using NUnit.Framework;

namespace KK.UI.UMG.Editor.Tests
{
    public sealed class AssetValidatorHashTests
    {
        private const string PackageId = "AssetValidatorHashTests";
        private const string SourceRoot = "Assets/UI/Source/" + PackageId;
        private const string GeneratedRoot = "Assets/UI/Generated/" + PackageId;
        private const string AssetPath = SourceRoot + "/Assets/icon.bytes";

        [SetUp]
        public void SetUp()
        {
            DeleteIfExists(SourceRoot);
            DeleteIfExists(GeneratedRoot);
            Directory.CreateDirectory(SourceRoot + "/Assets");
            File.WriteAllText(AssetPath, "asset");
        }

        [TearDown]
        public void TearDown()
        {
            DeleteIfExists(SourceRoot);
            DeleteIfExists(GeneratedRoot);
        }

        [Test]
        public void MissingContentHashSkipsHashValidation()
        {
            var context = CreateContext(null);

            new AssetValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "AST008"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "AST009"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "AST012"), Is.False);
        }

        [Test]
        public void MatchingContentHashPasses()
        {
            var context = CreateContext("sha256:" + ComputeSha256(AssetPath));

            new AssetValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "AST009"), Is.False);
            Assert.That(context.Issues.Any(issue => issue.Code == "AST012"), Is.False);
        }

        [Test]
        public void InvalidContentHashFormatReportsAst009()
        {
            var context = CreateContext("not-a-sha256-hash");

            new AssetValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "AST009"), Is.True);
            Assert.That(context.Issues.Any(issue => issue.Code == "AST012"), Is.False);
        }

        [Test]
        public void MismatchedContentHashReportsAst012()
        {
            var context = CreateContext("sha256:0000000000000000000000000000000000000000000000000000000000000000");

            new AssetValidator().Validate(context);

            Assert.That(context.Issues.Any(issue => issue.Code == "AST012"), Is.True);
        }

        private static KKUIPipelineContext CreateContext(string contentHash)
        {
            return new KKUIPipelineContext
            {
                SourceRoot = Path.GetFullPath(SourceRoot),
                GeneratedRoot = Path.GetFullPath(GeneratedRoot),
                Package = new UiPackageManifest
                {
                    PackageId = PackageId,
                    SharedAssetRoots = new List<string>()
                },
                Assets = new UiAssetsManifest
                {
                    Assets = new List<UiAssetSpec>
                    {
                        new UiAssetSpec
                        {
                            Id = "icon",
                            Type = "Texture",
                            Source = AssetPath,
                            Target = GeneratedRoot + "/Assets/icon.bytes",
                            ContentHash = contentHash
                        }
                    }
                },
                Layout = new UiLayoutManifest
                {
                    Root = new UiLayoutNode { Id = "Root", Type = "Panel" }
                }
            };
        }

        private static string ComputeSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
            }
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
    }
}
