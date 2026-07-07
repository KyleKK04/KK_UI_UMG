using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using KK.UI.UMG.Editor.Generators;
using KK.UI.UMG.Editor.Validators;

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
