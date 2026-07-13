using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KK.UI.UMG.Editor.Pipeline
{
    public sealed class ValidationLedgerWriter
    {
        public const string StartMarker = "<!-- ui-pipeline:validation-ledger:start -->";
        public const string EndMarker = "<!-- ui-pipeline:validation-ledger:end -->";

        private static readonly string[] Steps =
        {
            "Validate",
            "Generate",
            "Verify",
            "Preview",
            "Runtime"
        };

        private static readonly Dictionary<string, HashSet<string>> AllowedStatuses = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["Validate"] = new HashSet<string>(StringComparer.Ordinal) { "Pass", "Fail", "NotRun" },
            ["Generate"] = new HashSet<string>(StringComparer.Ordinal) { "Pass", "Fail", "Pending", "NotRun" },
            ["Verify"] = new HashSet<string>(StringComparer.Ordinal) { "Pass", "Fail", "NotRun" },
            ["Preview"] = new HashSet<string>(StringComparer.Ordinal) { "Pass", "Fail", "NotRun" },
            ["Runtime"] = new HashSet<string>(StringComparer.Ordinal) { "Pending", "Verified" }
        };

        public static string ValidationPath(KKUIPipelineContext context)
        {
            return Path.Combine(context.SourceRoot, "validation.md");
        }

        public void EnsureScaffold(KKUIPipelineContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.SourceRoot))
            {
                return;
            }

            Directory.CreateDirectory(context.SourceRoot);
            var path = ValidationPath(context);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, BuildNewDocument(context, DefaultRows(), null));
                return;
            }

            var text = File.ReadAllText(path);
            if (HasMarkers(text))
            {
                return;
            }

            var separator = text.EndsWith(Environment.NewLine, StringComparison.Ordinal)
                ? Environment.NewLine
                : Environment.NewLine + Environment.NewLine;
            File.WriteAllText(path, text + separator + BuildMarkedRegion(context, DefaultRows(), null) + Environment.NewLine);
        }

        public bool ValidateLedgerFile(string path, out string error)
        {
            error = null;
            if (!File.Exists(path))
            {
                error = "validation.md is missing.";
                return false;
            }

            var text = File.ReadAllText(path);
            if (!HasMarkers(text))
            {
                error = "validation.md is missing ui-pipeline validation ledger markers.";
                return false;
            }

            if (!TryReadRows(text, out var rows, out error))
            {
                return false;
            }

            foreach (var step in Steps)
            {
                if (!rows.TryGetValue(step, out var row))
                {
                    error = $"validation.md ledger is missing '{step}' row.";
                    return false;
                }

                if (!AllowedStatuses[step].Contains(row.Status))
                {
                    error = $"validation.md ledger has invalid status '{row.Status}' for '{step}'.";
                    return false;
                }
            }

            return true;
        }

        public void WritePipelineResult(KKUIPipelineContext context, KKUIPipelineResult result)
        {
            if (context == null || result == null)
            {
                return;
            }

            EnsureScaffold(context);
            var rows = ReadRowsOrDefault(context);
            var now = DateTime.UtcNow.ToString("O");

            switch (result.Operation)
            {
                case "Validate":
                    Update(rows, "Validate", result.Success ? "Pass" : "Fail", now, "KKUIPipeline", BuildIssueNote(result));
                    break;
                case "Generate":
                    WriteGenerateResult(rows, result, now);
                    break;
                case "Verify":
                    WriteVerifyResult(rows, result, now);
                    break;
            }

            WriteRows(context, rows, result);
        }

        public void WritePreviewResult(KKUIPipelineContext context, bool success, string note)
        {
            if (context == null)
            {
                return;
            }

            EnsureScaffold(context);
            var rows = ReadRowsOrDefault(context);
            Update(rows, "Preview", success ? "Pass" : "Fail", DateTime.UtcNow.ToString("O"), "KKUIPipelineWindow", string.IsNullOrWhiteSpace(note) ? "-" : note);
            WriteRows(context, rows, new KKUIPipelineResult
            {
                Operation = "Preview",
                Status = success ? "Succeeded" : "Failed",
                Success = success,
                Issues = new List<KKUIPipelineIssue>()
            });
        }

        public void WriteRuntimeStatus(KKUIPipelineContext context, bool verified, string source, string notes)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureScaffold(context);
            var path = ValidationPath(context);
            if (!ValidateLedgerFile(path, out var error))
            {
                throw new InvalidOperationException(error);
            }

            if (verified && (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(notes)))
            {
                throw new ArgumentException("Runtime verification requires a non-empty source and notes.");
            }

            var rows = ReadRowsOrDefault(context);
            if (verified && new[] { "Validate", "Generate", "Verify" }.Any(step => rows[step].Status != "Pass"))
            {
                throw new InvalidOperationException("Runtime can be marked Verified only after Validate, Generate, and Verify are Pass.");
            }

            rows["Runtime"] = new LedgerRow
            {
                Step = "Runtime",
                Status = verified ? "Verified" : "Pending",
                UpdatedAt = DateTime.UtcNow.ToString("O"),
                Source = string.IsNullOrWhiteSpace(source) ? "Manual" : source,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Runtime behavior requires re-verification." : notes
            };

            WriteRows(context, rows, new KKUIPipelineResult
            {
                Operation = "Runtime",
                Status = verified ? "Verified" : "Pending",
                Success = true,
                Issues = new List<KKUIPipelineIssue>()
            });
        }

        private static void WriteGenerateResult(Dictionary<string, LedgerRow> rows, KKUIPipelineResult result, string now)
        {
            Update(rows, "Runtime", "Pending", now, "KKUIPipeline", "Generate requires runtime re-verification.");
            var validationFailed = HasError(result, IsValidationIssue);
            Update(rows, "Validate", validationFailed ? "Fail" : "Pass", now, "KKUIPipeline", BuildIssueNote(result, IsValidationIssue));

            var generateFailed = validationFailed || HasError(result, IsGenerateIssue);
            var generateStatus = result.Status == "PendingCompile"
                ? "Pending"
                : generateFailed ? "Fail" : "Pass";
            Update(rows, "Generate", generateStatus, now, "KKUIPipeline", BuildIssueNote(result, IsGenerateIssue));

            if (HasError(result, IsVerifyIssue))
            {
                Update(rows, "Verify", "Fail", now, "GeneratedAssetVerifier", BuildIssueNote(result, IsVerifyIssue));
                return;
            }

            if (result.Success)
            {
                Update(rows, "Verify", "Pass", now, "GeneratedAssetVerifier", "-");
            }
        }

        private static void WriteVerifyResult(Dictionary<string, LedgerRow> rows, KKUIPipelineResult result, string now)
        {
            var validationFailed = HasError(result, IsValidationIssue);
            Update(rows, "Validate", validationFailed ? "Fail" : "Pass", now, "KKUIPipeline", BuildIssueNote(result, IsValidationIssue));
            Update(rows, "Verify", result.Success ? "Pass" : "Fail", now, "GeneratedAssetVerifier", BuildIssueNote(result));
        }

        private Dictionary<string, LedgerRow> ReadRowsOrDefault(KKUIPipelineContext context)
        {
            var path = ValidationPath(context);
            if (File.Exists(path) && TryReadRows(File.ReadAllText(path), out var rows, out _))
            {
                foreach (var step in Steps)
                {
                    if (!rows.ContainsKey(step))
                    {
                        rows[step] = DefaultRow(step);
                    }
                }

                return rows;
            }

            return DefaultRows();
        }

        private static Dictionary<string, LedgerRow> DefaultRows()
        {
            return Steps.ToDictionary(step => step, DefaultRow, StringComparer.Ordinal);
        }

        private static LedgerRow DefaultRow(string step)
        {
            return new LedgerRow
            {
                Step = step,
                Status = step == "Runtime" ? "Pending" : "NotRun",
                UpdatedAt = "-",
                Source = step == "Runtime" ? "Manual" : "KKUIPipeline",
                Notes = step == "Runtime" ? "Runtime behavior not verified" : "-"
            };
        }

        private void WriteRows(KKUIPipelineContext context, Dictionary<string, LedgerRow> rows, KKUIPipelineResult result)
        {
            var path = ValidationPath(context);
            var text = File.Exists(path) ? File.ReadAllText(path) : BuildNewDocument(context, rows, result);
            var region = BuildMarkedRegion(context, rows, result);

            if (!HasMarkers(text))
            {
                File.WriteAllText(path, BuildNewDocument(context, rows, result));
                return;
            }

            var start = text.IndexOf(StartMarker, StringComparison.Ordinal);
            var end = text.IndexOf(EndMarker, StringComparison.Ordinal);
            var afterEnd = end + EndMarker.Length;
            var updated = text.Substring(0, start) + region + text.Substring(afterEnd);
            File.WriteAllText(path, updated);
        }

        private static string BuildNewDocument(KKUIPipelineContext context, Dictionary<string, LedgerRow> rows, KKUIPipelineResult result)
        {
            var packageId = context?.Package?.PackageId ?? Path.GetFileName(context?.SourceRoot ?? "UI Package");
            return string.Join(Environment.NewLine, new[]
            {
                $"# {packageId} Validation",
                "",
                BuildMarkedRegion(context, rows, result),
                "",
                "## Manual Notes",
                "",
                "- Runtime behavior pending unless explicitly verified."
            }) + Environment.NewLine;
        }

        private static string BuildMarkedRegion(KKUIPipelineContext context, Dictionary<string, LedgerRow> rows, KKUIPipelineResult result)
        {
            var lines = new List<string>
            {
                StartMarker,
                "## Pipeline Ledger",
                "",
                "| Step | Status | Updated At | Source | Notes |",
                "|---|---|---|---|---|"
            };

            foreach (var step in Steps)
            {
                var row = rows.TryGetValue(step, out var existing) ? existing : DefaultRow(step);
                lines.Add($"| {Escape(row.Step)} | {Escape(row.Status)} | {Escape(row.UpdatedAt)} | {Escape(row.Source)} | {Escape(row.Notes)} |");
            }

            lines.Add("");
            lines.Add("## Last Operation");
            lines.Add("");
            lines.Add("| Item | Value |");
            lines.Add("|---|---|");
            lines.Add($"| Operation | {Escape(result?.Operation ?? "-")} |");
            lines.Add($"| Success | {Escape(result == null ? "-" : result.Success.ToString())} |");
            lines.Add($"| Issues | {Escape(result == null ? "-" : result.Issues.Count.ToString())} |");
            lines.Add($"| Generated Report | `{Escape(GeneratedReportPath(context))}` |");
            lines.Add(EndMarker);
            return string.Join(Environment.NewLine, lines);
        }

        private static string GeneratedReportPath(KKUIPipelineContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.GeneratedRoot))
            {
                return "-";
            }

            return AssetManifestUtility.ToAssetPath(Path.Combine(context.GeneratedRoot, "Reports", "generate-report.json"));
        }

        private static bool HasMarkers(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var start = text.IndexOf(StartMarker, StringComparison.Ordinal);
            var end = text.IndexOf(EndMarker, StringComparison.Ordinal);
            return start >= 0 && end > start;
        }

        private static bool TryReadRows(string text, out Dictionary<string, LedgerRow> rows, out string error)
        {
            rows = new Dictionary<string, LedgerRow>(StringComparer.Ordinal);
            error = null;

            var start = text.IndexOf(StartMarker, StringComparison.Ordinal);
            var end = text.IndexOf(EndMarker, StringComparison.Ordinal);
            if (start < 0 || end <= start)
            {
                error = "validation.md is missing ledger markers.";
                return false;
            }

            var body = text.Substring(start, end - start);
            foreach (var rawLine in body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("|", StringComparison.Ordinal) || !line.EndsWith("|", StringComparison.Ordinal))
                {
                    continue;
                }

                var cells = line.Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
                if (cells.Length < 5 ||
                    cells[0] == "Step" ||
                    cells[0].StartsWith("---", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!AllowedStatuses.ContainsKey(cells[0]))
                {
                    continue;
                }

                var step = cells[0];
                var status = step == "Runtime" && cells[1] == "Pass" ? "Verified" : cells[1];
                rows[step] = new LedgerRow
                {
                    Step = step,
                    Status = status,
                    UpdatedAt = cells[2],
                    Source = cells[3],
                    Notes = cells[4]
                };
            }

            return true;
        }

        private static void Update(Dictionary<string, LedgerRow> rows, string step, string status, string updatedAt, string source, string notes)
        {
            if (!rows.TryGetValue(step, out var row))
            {
                row = DefaultRow(step);
                rows[step] = row;
            }

            row.Status = status;
            row.UpdatedAt = updatedAt;
            row.Source = source;
            row.Notes = string.IsNullOrWhiteSpace(notes) ? "-" : notes;
        }

        private static bool HasError(KKUIPipelineResult result, Func<KKUIPipelineIssue, bool> predicate)
        {
            return result.Issues.Any(issue => issue.Severity == KKUIPipelineIssueSeverity.Error && predicate(issue));
        }

        private static bool IsValidationIssue(KKUIPipelineIssue issue)
        {
            return StartsWithAny(issue.Code, "PKG", "SRC", "AST", "LAY", "BND", "COD", "BUS", "LOC");
        }

        private static bool IsGenerateIssue(KKUIPipelineIssue issue)
        {
            return StartsWithAny(issue.Code, "PFB", "GEN");
        }

        private static bool IsVerifyIssue(KKUIPipelineIssue issue)
        {
            return StartsWithAny(issue.Code, "VER");
        }

        private static bool StartsWithAny(string value, params string[] prefixes)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static string BuildIssueNote(KKUIPipelineResult result)
        {
            return BuildIssueNote(result, _ => true);
        }

        private static string BuildIssueNote(KKUIPipelineResult result, Func<KKUIPipelineIssue, bool> predicate)
        {
            var issues = result.Issues.Where(predicate).Where(issue => issue.Severity == KKUIPipelineIssueSeverity.Error).ToList();
            if (issues.Count == 0)
            {
                return "-";
            }

            return string.Join(", ", issues.Select(issue => issue.Code));
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class LedgerRow
        {
            public string Step { get; set; }
            public string Status { get; set; }
            public string UpdatedAt { get; set; }
            public string Source { get; set; }
            public string Notes { get; set; }
        }
    }
}
