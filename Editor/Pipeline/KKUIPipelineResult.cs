using System.Collections.Generic;
using System.Linq;

namespace KK.UI.UMG.Editor.Pipeline
{
    public sealed class KKUIPipelineResult
    {
        public string Operation { get; set; }
        public string Status { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public IReadOnlyList<KKUIPipelineIssue> Issues { get; set; } = new List<KKUIPipelineIssue>();
        public IReadOnlyList<string> GeneratedFiles { get; set; } = new List<string>();

        public static KKUIPipelineResult FromContext(string operation, KKUIPipelineContext context, IEnumerable<string> generatedFiles)
        {
            var issues = context.Issues.ToList();
            return new KKUIPipelineResult
            {
                Operation = operation,
                Status = issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error) ? "Failed" : "Succeeded",
                Success = issues.All(issue => issue.Severity != KKUIPipelineIssueSeverity.Error),
                Issues = issues,
                GeneratedFiles = generatedFiles.Distinct().ToList()
            };
        }
    }
}
