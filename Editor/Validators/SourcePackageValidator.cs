using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class SourcePackageValidator : IManifestValidator
    {
        private static readonly Dictionary<string, string> ExpectedManifestFiles = new Dictionary<string, string>
        {
            ["layout"] = "layout.json",
            ["assets"] = "assets.json",
            ["bindings"] = "bindings.json",
            ["codegen"] = "codegen.json",
            ["strings"] = "strings.json"
        };

        private static readonly HashSet<string> IgnoredSourceAssetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".meta",
            ".DS_Store"
        };

        public void Validate(KKUIPipelineContext context)
        {
            ValidateSourceRoot(context);
            ValidatePackageManifestName(context);
            ValidateManifestReferences(context);
            ValidateLegacyStringFiles(context);
            ValidatePackageDocs(context);
            ValidateNoGeneratedArtifacts(context);
            ValidateSourceAssetsDeclared(context);
        }

        private static void ValidateSourceRoot(KKUIPipelineContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Package?.PackageId) || string.IsNullOrWhiteSpace(context.SourceRoot))
            {
                return;
            }

            var sourceRoot = AssetManifestUtility.NormalizeAssetPath(AssetManifestUtility.ToAssetPath(context.SourceRoot));
            if (!IsUnityAssetPath(sourceRoot))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC001", $"Source package root must be under Assets/ or Packages/, got '{sourceRoot}'.");
                return;
            }

            if (ContainsPathSegment(sourceRoot, "Generated"))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC001", $"Source package root must not be inside a Generated folder: '{sourceRoot}'.");
            }

            var folderName = Path.GetFileName(sourceRoot.TrimEnd('/'));
            if (!string.Equals(folderName, context.Package.PackageId, StringComparison.Ordinal))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC001", $"Source package root folder must match packageId '{context.Package.PackageId}', got '{folderName}'.");
            }
        }

        private static void ValidatePackageManifestName(KKUIPipelineContext context)
        {
            var fileName = Path.GetFileName(context.PackageManifestPath);
            if (!string.Equals(fileName, "package.json", StringComparison.OrdinalIgnoreCase))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC002", "Source package entry manifest must be named package.json.");
            }
        }

        private static void ValidateManifestReferences(KKUIPipelineContext context)
        {
            ValidateManifestReference(context, "layout", context.Package?.Manifests?.Layout);
            ValidateManifestReference(context, "assets", context.Package?.Manifests?.Assets);
            ValidateManifestReference(context, "bindings", context.Package?.Manifests?.Bindings);
            ValidateManifestReference(context, "codegen", context.Package?.Manifests?.Codegen);
            ValidateManifestReference(context, "strings", context.Package?.Manifests?.Strings);
        }

        private static void ValidateManifestReference(KKUIPipelineContext context, string name, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            if (!ExpectedManifestFiles.TryGetValue(name, out var expectedFile))
            {
                return;
            }

            var normalized = AssetManifestUtility.NormalizeAssetPath(relativePath);
            if (!string.Equals(normalized, expectedFile, StringComparison.Ordinal))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC003", $"Manifest reference '{name}' must be '{expectedFile}' for v0.5.1 Source packages.");
            }

            if (Path.IsPathRooted(relativePath) || normalized.Contains("../") || normalized.StartsWith("..", StringComparison.Ordinal))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC004", $"Manifest reference '{name}' must be a file inside the Source package root.");
                return;
            }

            var resolved = Path.GetFullPath(Path.Combine(context.SourceRoot, relativePath));
            var sourceRoot = Path.GetFullPath(context.SourceRoot);
            if (!IsUnderDirectory(resolved, sourceRoot))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC004", $"Manifest reference '{name}' must be a file inside the Source package root.");
            }
        }

        private static void ValidateLegacyStringFiles(KKUIPipelineContext context)
        {
            var legacyPath = Path.Combine(context.SourceRoot, "strings.zh-Hans.json");
            if (File.Exists(legacyPath))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC005", "Legacy strings.zh-Hans.json must not exist in v0.5 Source packages. Use strings.json.");
            }
        }

        private static void ValidatePackageDocs(KKUIPipelineContext context)
        {
            var readmePath = Path.Combine(context.SourceRoot, "README.md");
            if (!File.Exists(readmePath))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC008", "Source package must include README.md.");
            }

            var validationPath = ValidationLedgerWriter.ValidationPath(context);
            if (!new ValidationLedgerWriter().ValidateLedgerFile(validationPath, out var error))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "SRC009", $"Source package validation.md ledger is invalid: {error}");
            }
        }

        private static void ValidateNoGeneratedArtifacts(KKUIPipelineContext context)
        {
            if (!Directory.Exists(context.SourceRoot))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(context.SourceRoot, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var normalized = AssetManifestUtility.NormalizeAssetPath(file);
                if (normalized.EndsWith(".Generated.cs", StringComparison.OrdinalIgnoreCase) ||
                    normalized.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                    normalized.EndsWith("/generate-report.json", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Contains("/Generated/") ||
                    normalized.Contains("/Reports/"))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "SRC006", $"Generated artifact '{AssetManifestUtility.ToAssetPath(file)}' must not be inside Source.");
                }
            }
        }

        private static void ValidateSourceAssetsDeclared(KKUIPipelineContext context)
        {
            var sourceAssetsRoot = AssetManifestUtility.SourceAssetsRoot(context);
            if (!Directory.Exists(sourceAssetsRoot))
            {
                return;
            }

            var declaredSources = new HashSet<string>(
                (context.Assets?.Assets ?? new List<UiAssetSpec>())
                    .Select(asset => AssetManifestUtility.NormalizeAssetPath(asset.Source)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(sourceAssetsRoot, "*", SearchOption.AllDirectories))
            {
                var normalized = AssetManifestUtility.NormalizeAssetPath(file);
                var extension = Path.GetExtension(normalized);
                if (IgnoredSourceAssetExtensions.Contains(extension) || normalized.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!declaredSources.Contains(normalized))
                {
                    context.Add(KKUIPipelineIssueSeverity.Warning, "SRC007", $"Source asset '{normalized}' is not declared in assets.json and will not be copied to Generated.");
                }
            }
        }

        private static bool IsUnderDirectory(string path, string directory)
        {
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(normalizedPath, normalizedDirectory, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(normalizedDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(normalizedDirectory + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnityAssetPath(string assetPath)
        {
            return assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                assetPath.StartsWith("Packages/", StringComparison.Ordinal);
        }

        private static bool ContainsPathSegment(string assetPath, string segment)
        {
            return assetPath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase));
        }
    }
}
