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
}
