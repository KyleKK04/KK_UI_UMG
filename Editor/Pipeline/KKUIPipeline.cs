using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using KK.UI.UMG.Editor.Generators;
using KK.UI.UMG.Editor.Validators;
using KK.UI.UMG.Internal;

namespace KK.UI.UMG.Editor.Pipeline
{
    public sealed class KKUIPipeline
    {
        private readonly IManifestValidator[] _validators =
        {
            new PackageValidator(),
            new SourcePackageValidator(),
            new AssetValidator(),
            new LayoutValidator(),
            new BindingValidator(),
            new CodegenValidator(),
            new BusChannelValidator(),
            new LocKeyValidator()
        };

        public KKUIPipelineResult Run(string packageManifestPath, string generatedParentPath = null)
        {
            try
            {
                var context = KKUIPipelineContext.Load(packageManifestPath, generatedParentPath);
                new ValidationLedgerWriter().EnsureScaffold(context);
                Validate(context);
                if (context.Issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error))
                {
                    var failed = KKUIPipelineResult.FromContext("Generate", context, Array.Empty<string>());
                    new ReportWriter().Write(context, failed);
                    new ValidationLedgerWriter().WritePipelineResult(context, failed);
                    return failed;
                }

                var generated = new List<string>();
                generated.AddRange(new UIAssetImporter().Import(context));
                generated.AddRange(new CSharpCodeGenerator().Generate(context));
                AssetDatabase.Refresh();

                try
                {
                    generated.Add(new UguiPrefabGenerator().Generate(context));
                }
                catch (Exception ex)
                {
                    if (IsCompileDeferred(ex))
                    {
                        if (!PendingPrefabGenerationScheduler.IsRunningContinuation)
                        {
                            PendingPrefabGenerationScheduler.Schedule(packageManifestPath, generatedParentPath);
                        }

                        context.Add(KKUIPipelineIssueSeverity.Info, "GENPENDING", $"{ex.Message} Unity is compiling generated scripts; prefab generation will continue automatically after compilation finishes.");
                        var pending = KKUIPipelineResult.FromContext("Generate", context, generated);
                        pending.Status = "PendingCompile";
                        pending.Success = false;
                        generated.AddRange(new ReportWriter().Write(context, pending));
                        new ValidationLedgerWriter().WritePipelineResult(context, pending);
                        AssetDatabase.Refresh();
                        return pending;
                    }

                    context.Add(KKUIPipelineIssueSeverity.Error, "PFB001", $"Prefab generation failed: {ex.Message}");
                }

                if (!context.Issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error))
                {
                    generated.AddRange(new GeneratedAssetVerifier().Verify(context));
                }
                var result = KKUIPipelineResult.FromContext("Generate", context, generated);
                generated.AddRange(new ReportWriter().Write(context, result));
                new ValidationLedgerWriter().WritePipelineResult(context, result);
                AssetDatabase.Refresh();
                return KKUIPipelineResult.FromContext("Generate", context, generated);
            }
            catch (Exception ex)
            {
                return new KKUIPipelineResult { Operation = "Generate", Status = "Failed", Success = false, Error = ex.Message };
            }
        }

        public KKUIPipelineResult ValidateOnly(string packageManifestPath, string generatedParentPath = null)
        {
            try
            {
                var context = KKUIPipelineContext.Load(packageManifestPath, generatedParentPath);
                new ValidationLedgerWriter().EnsureScaffold(context);
                Validate(context);
                var result = KKUIPipelineResult.FromContext("Validate", context, Array.Empty<string>());
                new ReportWriter().Write(context, result);
                new ValidationLedgerWriter().WritePipelineResult(context, result);
                return result;
            }
            catch (Exception ex)
            {
                return new KKUIPipelineResult { Operation = "Validate", Status = "Failed", Success = false, Error = ex.Message };
            }
        }

        public KKUIPipelineResult VerifyOnly(string packageManifestPath, string generatedParentPath = null)
        {
            try
            {
                var context = KKUIPipelineContext.Load(packageManifestPath, generatedParentPath);
                new ValidationLedgerWriter().EnsureScaffold(context);
                Validate(context);
                var verified = context.Issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error)
                    ? Array.Empty<string>()
                    : new GeneratedAssetVerifier().Verify(context);
                var result = KKUIPipelineResult.FromContext("Verify", context, verified);
                new ReportWriter().Write(context, result);
                new ValidationLedgerWriter().WritePipelineResult(context, result);
                return result;
            }
            catch (Exception ex)
            {
                return new KKUIPipelineResult { Operation = "Verify", Status = "Failed", Success = false, Error = ex.Message };
            }
        }

        public KKUIPipelineResult PrefabToJson(string packageManifestPath, string generatedParentPath = null)
        {
            try
            {
                var context = KKUIPipelineContext.Load(packageManifestPath, generatedParentPath);
                Validate(context);
                if (context.Issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error))
                {
                    return KKUIPipelineResult.FromContext("Prefab to JSON", context, Array.Empty<string>());
                }

                AssetDatabase.SaveAssets();
                var changedNodes = new PrefabLayoutExporter().Export(context);
                var layoutPath = Path.Combine(context.SourceRoot, context.Package.Manifests.Layout);
                context.Add(
                    KKUIPipelineIssueSeverity.Info,
                    "P2J001",
                    changedNodes.Count == 0
                        ? "Prefab layout already matches layout.json; no file was written."
                        : $"Captured {changedNodes.Count} layout node(s) from the saved prefab into '{AssetManifestUtility.ToAssetPath(layoutPath)}'.");
                return KKUIPipelineResult.FromContext(
                    "Prefab to JSON",
                    context,
                    changedNodes.Count == 0 ? Array.Empty<string>() : new[] { layoutPath });
            }
            catch (Exception ex)
            {
                return new KKUIPipelineResult
                {
                    Operation = "Prefab to JSON",
                    Status = "Failed",
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public KKUIPipelineResult ExportPrefab(string prefabAssetPath, string fallbackPackageManifestPath = null)
        {
            try
            {
                var prefabPath = AssetManifestUtility.NormalizeAssetPath(prefabAssetPath);
                if (string.IsNullOrWhiteSpace(prefabPath) ||
                    !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Select a generated Prefab in the Project window before clicking Export.");
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var marker = prefab != null ? prefab.GetComponent<GeneratedAssetMarker>() : null;
                if (marker == null || string.IsNullOrWhiteSpace(marker.PackageId))
                {
                    throw new InvalidOperationException("The selected asset is not a KKPipeline generated Prefab.");
                }

                var generatedParentPath = ResolveGeneratedParentPath(prefabPath, marker.PackageId);
                var packageManifestPath = ResolveSourceManifestPath(
                    prefabPath,
                    generatedParentPath,
                    marker,
                    fallbackPackageManifestPath);
                var result = PrefabToJson(packageManifestPath, generatedParentPath);
                result.Operation = "Export";
                return result;
            }
            catch (Exception ex)
            {
                return new KKUIPipelineResult
                {
                    Operation = "Export",
                    Status = "Failed",
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private static string ResolveGeneratedParentPath(string prefabPath, string packageId)
        {
            var prefabsRoot = AssetManifestUtility.NormalizeAssetPath(Path.GetDirectoryName(prefabPath));
            var generatedRoot = AssetManifestUtility.NormalizeAssetPath(Path.GetDirectoryName(prefabsRoot));
            var generatedParent = AssetManifestUtility.NormalizeAssetPath(Path.GetDirectoryName(generatedRoot));
            if (!string.Equals(Path.GetFileName(prefabsRoot), "Prefabs", StringComparison.Ordinal) ||
                !string.Equals(Path.GetFileName(generatedRoot), packageId, StringComparison.Ordinal) ||
                !string.Equals(Path.GetFileName(prefabPath), $"{packageId}View.prefab", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(generatedParent))
            {
                throw new InvalidOperationException(
                    $"The selected Prefab must be '{packageId}/Prefabs/{packageId}View.prefab'.");
            }

            return generatedParent;
        }

        private static string ResolveSourceManifestPath(
            string prefabPath,
            string generatedParentPath,
            GeneratedAssetMarker marker,
            string fallbackPackageManifestPath)
        {
            var candidates = new[] { marker.SourceManifestPath, fallbackPackageManifestPath }
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(AssetManifestUtility.NormalizeAssetPath)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates)
            {
                try
                {
                    var context = KKUIPipelineContext.Load(candidate, generatedParentPath);
                    var expectedPrefabPath = AssetManifestUtility.ToAssetPath(
                        Path.Combine(context.GeneratedRoot, "Prefabs", $"{context.Package.PackageId}View.prefab"));
                    if (string.Equals(context.Package.PackageId, marker.PackageId, StringComparison.Ordinal) &&
                        string.Equals(expectedPrefabPath, prefabPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }
                catch
                {
                    // Try the next known Source manifest.
                }
            }

            throw new InvalidOperationException(
                "Cannot resolve the Source package for the selected Prefab. Switch to Import, select its package.json, run Generate once, then retry Export.");
        }

        private static bool IsCompileDeferred(Exception ex)
        {
            return ex.Message.Contains("not compiled yet") || ex.Message.Contains("out of date");
        }

        private void Validate(KKUIPipelineContext context)
        {
            foreach (var validator in _validators)
            {
                validator.Validate(context);
            }
        }
    }
}
