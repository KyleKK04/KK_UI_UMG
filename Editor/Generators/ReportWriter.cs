using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Generators
{
    public sealed class ReportWriter
    {
        public IReadOnlyList<string> Write(KKUIPipelineContext context, KKUIPipelineResult result)
        {
            var reportsRoot = Path.Combine(context.GeneratedRoot, "Reports");
            Directory.CreateDirectory(reportsRoot);

            var jsonPath = Path.Combine(reportsRoot, "generate-report.json");
            var mdPath = Path.Combine(reportsRoot, "validation.md");

            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(new
            {
                pipelineVersion = "0.9",
                packageId = context.Package.PackageId,
                operation = result.Operation,
                status = result.Status,
                success = result.Success,
                busValidation = new
                {
                    inRoutes = context.Codegen.Bus?.Routes?.Count ?? 0,
                    outChannels = context.Bindings.Events.Count(evt => !string.IsNullOrWhiteSpace(evt.Channel)),
                    issues = result.Issues.Where(issue => issue.Code.StartsWith("BUS", StringComparison.Ordinal)).ToList()
                },
                localizationValidation = new
                {
                    defaultCulture = context.Strings.DefaultCulture,
                    cultures = context.Strings.Cultures,
                    declaredKeys = context.Strings.Strings.Count,
                    usedKeys = GetUsedLocKeys(context).Count,
                    issues = result.Issues.Where(issue => issue.Code.StartsWith("LOC", StringComparison.Ordinal)).ToList()
                },
                componentStats = GetComponentStats(context),
                layoutComponentStats = GetLayoutComponentStats(context),
                generatedAt = DateTime.UtcNow.ToString("O"),
                generatedFiles = result.GeneratedFiles,
                issues = result.Issues
            }, Formatting.Indented));

            File.WriteAllText(mdPath, BuildMarkdown(context, result));
            return new[] { jsonPath, mdPath };
        }

        private static string BuildMarkdown(KKUIPipelineContext context, KKUIPipelineResult result)
        {
            var lines = new List<string>
            {
                "# KK_UI_UMG Validation",
                "",
                $"- Manifest: `{context.PackageManifestPath}`",
                $"- Operation: `{result.Operation}`",
                $"- Status: `{result.Status}`",
                $"- Success: `{result.Success}`",
                "",
                "## Issues"
            };

            if (!result.Issues.Any())
            {
                lines.Add("- None");
            }
            else
            {
                lines.AddRange(result.Issues.Select(issue => $"- {issue.Severity} `{issue.Code}`: {issue.Message}"));
            }

            lines.Add("");
            lines.Add("## Bus");
            lines.AddRange(BuildBusLines(context));
            lines.Add("");
            lines.Add("## Localization");
            lines.AddRange(BuildLocalizationLines(context));
            lines.Add("");
            lines.Add("## Components");
            lines.AddRange(GetComponentStats(context).Select(stat => $"- `{stat.Key}`: `{stat.Value}`"));
            lines.Add("");
            lines.Add("## Layout Components");
            var layoutStats = GetLayoutComponentStats(context);
            lines.AddRange(layoutStats.Any() ? layoutStats.Select(stat => $"- `{stat.Key}`: `{stat.Value}`") : new[] { "- None" });
            lines.Add("");
            lines.Add("## Generated Files");
            lines.AddRange(result.GeneratedFiles.Any() ? result.GeneratedFiles.Select(file => $"- `{file}`") : new[] { "- None" });
            return string.Join(Environment.NewLine, lines);
        }

        private static IEnumerable<string> BuildBusLines(KKUIPipelineContext context)
        {
            var lines = new List<string>();
            var routes = context.Codegen.Bus?.Routes ?? new List<UiBusRouteSpec>();
            lines.Add($"- Inbound Routes: `{routes.Count}`");
            foreach (var route in routes)
            {
                lines.Add($"- `{BusChannelUtility.BuildFullChannel(context.Package.PackageId, route.Channel)}` -> `{route.Action}` via `UIManager`");
            }

            var outboundEvents = context.Bindings.Events.Where(evt => !string.IsNullOrWhiteSpace(evt.Channel)).ToList();
            lines.Add($"- Outbound Channels: `{outboundEvents.Count}`");
            foreach (var evt in outboundEvents)
            {
                var fields = evt.ChannelPayloadFields == null || evt.ChannelPayloadFields.Count == 0
                    ? ""
                    : $" fields: `{string.Join("`, `", evt.ChannelPayloadFields)}`";
                lines.Add($"- `{BusChannelUtility.BuildFullChannel(context.Package.PackageId, evt.Channel)}` from `{evt.Handler}`{fields}");
            }

            return lines;
        }

        private static IEnumerable<string> BuildLocalizationLines(KKUIPipelineContext context)
        {
            var usedKeys = GetUsedLocKeys(context);
            var lines = new List<string>
            {
                $"- Default Culture: `{context.Strings.DefaultCulture}`",
                $"- Cultures: `{string.Join("`, `", context.Strings.Cultures ?? new List<string>())}`",
                $"- Declared Keys: `{context.Strings.Strings.Count}`",
                $"- Used Keys: `{usedKeys.Count}`"
            };

            foreach (var key in usedKeys)
            {
                lines.Add($"- `{key}`");
            }

            return lines;
        }

        private static List<string> GetUsedLocKeys(KKUIPipelineContext context)
        {
            var keys = new List<string>();
            Walk(context.Layout.Root, node =>
            {
                if (node.Type == "Text" && !string.IsNullOrWhiteSpace(node.Text?.LocKey))
                {
                    keys.Add(node.Text.LocKey);
                }
            });
            return keys;
        }

        private static Dictionary<string, int> GetComponentStats(KKUIPipelineContext context)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            Walk(context.Layout.Root, node =>
            {
                if (string.IsNullOrWhiteSpace(node.Type))
                {
                    return;
                }

                result.TryGetValue(node.Type, out var count);
                result[node.Type] = count + 1;
            });
            return result;
        }

        private static Dictionary<string, int> GetLayoutComponentStats(KKUIPipelineContext context)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            Walk(context.Layout.Root, node =>
            {
                var layout = node.LayoutComponents;
                if (layout == null)
                {
                    return;
                }

                AddIfPresent(result, "LayoutElement", layout.LayoutElement);
                AddIfPresent(result, "HorizontalLayoutGroup", layout.HorizontalLayout);
                AddIfPresent(result, "VerticalLayoutGroup", layout.VerticalLayout);
                AddIfPresent(result, "GridLayoutGroup", layout.GridLayout);
                AddIfPresent(result, "ContentSizeFitter", layout.ContentSizeFitter);
                AddIfPresent(result, "AspectRatioFitter", layout.AspectRatioFitter);
            });
            return result;
        }

        private static void AddIfPresent(Dictionary<string, int> result, string key, object value)
        {
            if (value == null)
            {
                return;
            }

            result.TryGetValue(key, out var count);
            result[key] = count + 1;
        }

        private static void Walk(UiLayoutNode node, Action<UiLayoutNode> visitor)
        {
            if (node == null)
            {
                return;
            }

            visitor(node);
            foreach (var child in node.Children ?? new List<UiLayoutNode>())
            {
                Walk(child, visitor);
            }

            if (node.Type == "VerticalList" && node.VerticalList?.ItemTemplate != null)
            {
                Walk(node.VerticalList.ItemTemplate, visitor);
            }
        }
    }
}
