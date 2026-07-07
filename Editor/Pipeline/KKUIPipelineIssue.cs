namespace KK.UI.UMG.Editor.Pipeline
{
    public enum KKUIPipelineIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class KKUIPipelineIssue
    {
        public KKUIPipelineIssueSeverity Severity { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public static class IssueHintCatalog
    {
        public static string GetHint(string code)
        {
            switch (code)
            {
                case "TXT001":
                    return "Move static Text copy into strings.json and reference it with layout.json locKey.";
                case "TXT002":
                    return "Add the missing locKey to strings.json for the defaultCulture.";
                case "TXT003":
                    return "Use either static locKey text or a dynamic text binding, not both on the same Text node.";
                case "TXT004":
                    return "Button labels that do not change at runtime should use locKey instead of Store fields.";
                case "TXT005":
                    return "Remove unused string keys or reference them from layout.json locKey.";
                case "AST004":
                    return "Use an existing Unity asset path under Assets/ or Packages/.";
                case "AST005":
                    return "Place package-owned assets under Source/Assets or add a narrow sharedAssetRoots entry.";
                case "AST006":
                    return "Package-owned asset targets must resolve under the Generated Assets folder.";
                case "AST008":
                    return "contentHash is optional in v1.0.x; copy the reported sha256 value if you want strict asset verification.";
                case "AST009":
                case "AST012":
                    return "Regenerate or update the sha256 contentHash after changing the source asset.";
                case "GEN003":
                    return "Use Assets/UI/Generated/<PackageId> or select a Generated Parent Folder override in KKPipeline.";
                case "GEN006":
                    return "Set codegen.addressablesKey to UI/<PackageId>/<PackageId>View.";
                case "GEN007":
                    return "Use supported framework base classes: UIViewBase for View and UIControllerBase for Controller.";
                case "GEN008":
                case "GEN009":
                    return "Generated Parent must be inside this project and output to <Generated Parent>/<PackageId>.";
                case "CG020":
                case "CG021":
                case "CG022":
                case "CG023":
                    return "Fix requiredServices so each service has a valid unique property name and UI-facing type.";
                case "GENPENDING":
                    return "Wait for Unity compilation; prefab generation will continue automatically.";
                default:
                    return null;
            }
        }
    }
}
