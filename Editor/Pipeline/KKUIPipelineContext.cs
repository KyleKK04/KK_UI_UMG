using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using KK.UI.UMG.Editor.Manifests;

namespace KK.UI.UMG.Editor.Pipeline
{
    public sealed class KKUIPipelineContext
    {
        public string PackageManifestPath { get; set; }
        public string SourceRoot { get; set; }
        public string GeneratedParentOverride { get; set; }
        public string GeneratedRoot { get; set; }
        public UiPackageManifest Package { get; set; }
        public UiAssetsManifest Assets { get; set; }
        public UiLayoutManifest Layout { get; set; }
        public UiBindingsManifest Bindings { get; set; }
        public UiCodegenManifest Codegen { get; set; }
        public UiStringsManifest Strings { get; set; }
        public List<KKUIPipelineIssue> Issues { get; } = new List<KKUIPipelineIssue>();

        public bool HasGeneratedParentOverride => !string.IsNullOrWhiteSpace(GeneratedParentOverride);

        public static KKUIPipelineContext Load(string packageManifestPath, string generatedParentPath = null)
        {
            var fullPackagePath = Path.GetFullPath(packageManifestPath);
            var sourceRoot = Path.GetDirectoryName(fullPackagePath);
            var package = ReadJson<UiPackageManifest>(fullPackagePath);
            var codegen = ReadJson<UiCodegenManifest>(Path.Combine(sourceRoot, package.Manifests.Codegen));
            var generatedParentOverride = NormalizeGeneratedParentPath(generatedParentPath);
            var generatedRoot = string.IsNullOrWhiteSpace(generatedParentOverride)
                ? Path.GetFullPath(Path.Combine(sourceRoot, codegen.OutputRoot))
                : Path.GetFullPath(Path.Combine(generatedParentOverride, package.PackageId));

            return new KKUIPipelineContext
            {
                PackageManifestPath = fullPackagePath,
                SourceRoot = sourceRoot,
                GeneratedParentOverride = generatedParentOverride,
                GeneratedRoot = generatedRoot,
                Package = package,
                Assets = ReadOptionalJson<UiAssetsManifest>(sourceRoot, package.Manifests.Assets) ?? new UiAssetsManifest(),
                Layout = ReadJson<UiLayoutManifest>(Path.Combine(sourceRoot, package.Manifests.Layout)),
                Bindings = ReadJson<UiBindingsManifest>(Path.Combine(sourceRoot, package.Manifests.Bindings)),
                Codegen = codegen,
                Strings = ReadJson<UiStringsManifest>(Path.Combine(sourceRoot, package.Manifests.Strings))
            };
        }

        public void Add(KKUIPipelineIssueSeverity severity, string code, string message)
        {
            Issues.Add(new KKUIPipelineIssue { Severity = severity, Code = code, Message = message });
        }

        private static T ReadJson<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        private static string NormalizeGeneratedParentPath(string generatedParentPath)
        {
            if (string.IsNullOrWhiteSpace(generatedParentPath))
            {
                return null;
            }

            return Path.GetFullPath(generatedParentPath.Replace('\\', '/').Trim().TrimEnd('/'));
        }

        private static T ReadOptionalJson<T>(string sourceRoot, string relativePath) where T : class
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var path = Path.Combine(sourceRoot, relativePath);
            return File.Exists(path) ? ReadJson<T>(path) : null;
        }
    }
}
