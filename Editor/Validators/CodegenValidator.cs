using System.Collections.Generic;
using System.IO;
using System.Linq;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class CodegenValidator : IManifestValidator
    {
        public void Validate(KKUIPipelineContext context)
        {
            if (context.Codegen.SchemaVersion != "1.0")
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN001", "codegen schemaVersion must be 1.0.");
            }

            if (context.Codegen.Namespace != context.Package.Namespace)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN002", "codegen namespace must match package namespace.");
            }

            ValidateClass(context, context.Codegen.View?.ClassName, "view");
            ValidateClass(context, context.Codegen.Controller?.ClassName, "controller");
            ValidateClass(context, context.Codegen.ViewModel?.ClassName, "viewModel");
            ValidateBaseClass(context, context.Codegen.View?.BaseClass, "UIViewBase", "view");
            ValidateBaseClass(context, context.Codegen.Controller?.BaseClass, "UIControllerBase", "controller");

            var expectedAddress = $"UI/{context.Package.PackageId}/{context.Package.PackageId}View";
            if (context.Codegen.AddressablesKey != expectedAddress)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN006", $"addressablesKey must be '{expectedAddress}'.");
            }

            var outputRoot = Path.GetFullPath(context.GeneratedRoot);
            var sourceRoot = Path.GetFullPath(context.SourceRoot);
            var expectedSuffix = Path.Combine("Assets", "UI", "Generated", context.Package.PackageId);
            if (!context.HasGeneratedParentOverride && !outputRoot.Replace('\\', '/').EndsWith(expectedSuffix.Replace('\\', '/')))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN003", $"outputRoot '{context.Codegen.OutputRoot}' must resolve to Assets/UI/Generated/{context.Package.PackageId}.");
            }

            if (context.HasGeneratedParentOverride)
            {
                ValidateGeneratedParentOverride(context, outputRoot);
            }

            if (IsUnderDirectory(outputRoot, sourceRoot))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN005", $"outputRoot '{context.Codegen.OutputRoot}' must not be inside Source/.");
            }

            ValidateRequiredServices(context);
        }

        private static void ValidateClass(KKUIPipelineContext context, string className, string label)
        {
            if (!ValidatorUtility.IsCSharpIdentifier(className))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN004", $"Invalid {label} class name '{className}'.");
            }
        }

        private static void ValidateBaseClass(KKUIPipelineContext context, string actual, string expected, string label)
        {
            if (actual != expected)
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN007", $"{label} baseClass must be '{expected}'.");
            }
        }

        private static void ValidateRequiredServices(KKUIPipelineContext context)
        {
            var services = context.Codegen.RequiredServices ?? new List<UiRequiredServiceSpec>();
            var seenProperties = new HashSet<string>();
            var reservedMembers = GetReservedControllerMembers(context);

            foreach (var service in services)
            {
                if (string.IsNullOrWhiteSpace(service?.Type))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "CG020", "requiredServices[].type cannot be null or empty.");
                }

                if (!ValidatorUtility.IsCSharpIdentifier(service?.Property))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "CG021", $"requiredServices[].property '{service?.Property}' must be a valid C# identifier.");
                    continue;
                }

                if (!seenProperties.Add(service.Property))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "CG022", $"requiredServices[].property '{service.Property}' is duplicated.");
                }

                if (reservedMembers.Contains(service.Property))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "CG023", $"requiredServices[].property '{service.Property}' conflicts with a generated Controller member.");
                }
            }
        }

        private static void ValidateGeneratedParentOverride(KKUIPipelineContext context, string outputRoot)
        {
            var generatedParent = Path.GetFullPath(context.GeneratedParentOverride);
            var expectedOutputRoot = Path.GetFullPath(Path.Combine(generatedParent, context.Package.PackageId));
            if (!string.Equals(outputRoot, expectedOutputRoot, System.StringComparison.OrdinalIgnoreCase))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN008", $"Generated output root must be '<Generated Parent>/{context.Package.PackageId}'.");
            }

            var parentAssetPath = AssetManifestUtility.NormalizeAssetPath(AssetManifestUtility.ToAssetPath(generatedParent));
            if (!parentAssetPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase) &&
                !parentAssetPath.StartsWith("Packages/", System.StringComparison.OrdinalIgnoreCase))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "GEN009", "Generated Parent must be inside this Unity project under Assets/ or Packages/.");
            }
        }

        private static bool IsUnderDirectory(string path, string directory)
        {
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return normalizedPath.StartsWith(normalizedDirectory, System.StringComparison.OrdinalIgnoreCase);
        }

        private static HashSet<string> GetReservedControllerMembers(KKUIPipelineContext context)
        {
            var members = new HashSet<string>
            {
                context.Codegen.Controller?.ClassName,
                "SystemId",
                "UIManager",
                "Store",
                "Binder",
                "View",
                "BindView",
                "Initialize",
                "Close",
                "OnPreOpen",
                "OnOpened",
                "OnActivated",
                "OnDeactivated",
                "OnPreClose",
                "OnClosed",
                "Flush",
                "Dispose",
                "OnViewBound",
                "OnGeneratedInitialize",
                "OnGeneratedEvent",
                "OnInitializeCore"
            };

            foreach (var evt in context.Bindings?.Events ?? Enumerable.Empty<UiEventSpec>())
            {
                if (!string.IsNullOrWhiteSpace(evt.Handler))
                {
                    members.Add(evt.Handler);
                    members.Add($"{evt.Handler}Core");
                }
            }

            return members;
        }
    }
}
