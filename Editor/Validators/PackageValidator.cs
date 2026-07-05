using System.IO;
using System.Collections.Generic;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class PackageValidator : IManifestValidator
    {
        public void Validate(KKUIPipelineContext context)
        {
            if (context.Package.SchemaVersion != "1.0")
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "PKG001", "package schemaVersion must be 1.0.");
            }

            if (string.IsNullOrWhiteSpace(context.Package.PackageId))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "PKG002", "packageId is required.");
            }

            if (!ValidatorUtility.IsNamespace(context.Package.Namespace))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "PKG003", $"Invalid namespace '{context.Package.Namespace}'.");
            }

            RequireManifest(context, context.Package.Manifests.Layout, "layout");
            RequireManifest(context, context.Package.Manifests.Assets, "assets");
            RequireManifest(context, context.Package.Manifests.Bindings, "bindings");
            RequireManifest(context, context.Package.Manifests.Codegen, "codegen");
            RequireManifest(context, context.Package.Manifests.Strings, "strings");

            foreach (var root in context.Package.SharedAssetRoots ?? new List<string>())
            {
                var normalized = NormalizeAssetPath(root);
                if (!normalized.StartsWith("Assets/") || !Directory.Exists(normalized))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "PKG006", $"sharedAssetRoots entry '{root}' must be an existing Assets/ directory.");
                    continue;
                }

                if (!IsSpecificSharedRoot(normalized))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "PKG007", $"sharedAssetRoots entry '{root}' is too broad. Use a specific directory such as Assets/SharedUI/Fonts/.");
                }
            }

            var controls = context.Package.V1 != null && context.Package.V1.Controls != null
                ? context.Package.V1.Controls
                : new List<string>();
            foreach (var control in controls)
            {
                if (!ValidatorUtility.SupportedControls.Contains(control))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "PKG004", $"Unsupported v1 control '{control}'.");
                }
            }
        }

        private static void RequireManifest(KKUIPipelineContext context, string relativePath, string name)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || !File.Exists(Path.Combine(context.SourceRoot, relativePath)))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "PKG005", $"Manifest reference '{name}' is missing or does not exist.");
            }
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        }

        private static bool IsSpecificSharedRoot(string normalized)
        {
            if (normalized == "Assets")
            {
                return false;
            }

            var segments = normalized.Split('/');
            return segments.Length >= 3;
        }
    }
}
