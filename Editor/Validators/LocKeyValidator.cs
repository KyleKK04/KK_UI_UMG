using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KK.UI.UMG.Editor.Manifests;
using KK.UI.UMG.Editor.Pipeline;

namespace KK.UI.UMG.Editor.Validators
{
    public sealed class LocKeyValidator : IManifestValidator
    {
        private static readonly Regex KeyRegex = new Regex(@"^[a-z0-9_.]+$", RegexOptions.Compiled);

        public void Validate(KKUIPipelineContext context)
        {
            ValidateStringManifest(context);

            var usedKeys = new HashSet<string>();
            var reportedMissingKeys = new HashSet<string>();

            ValidatorUtility.Walk(context.Layout.Root, node =>
            {
                AddLayoutLocKeys(node, usedKeys);

                if (node.Type != "Text" || node.Text == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(node.Text.Value) && !string.IsNullOrWhiteSpace(node.Text.LocKey))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "LOC006", $"Text node '{node.Id}' cannot define both value and locKey.");
                }

                if (string.IsNullOrWhiteSpace(node.Text.LocKey))
                {
                    if (!string.IsNullOrWhiteSpace(node.Text.Value))
                    {
                        context.Add(KKUIPipelineIssueSeverity.Warning, "TXT001", $"Text node '{node.Id}' has static literal text and should use locKey with strings.json.");
                    }
                    return;
                }

                if (!HasDefaultTranslation(context, node.Text.LocKey))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "TXT002", $"Text node '{node.Id}' locKey '{node.Text.LocKey}' must exist in strings.json defaultCulture '{context.Strings.DefaultCulture}'.");
                    reportedMissingKeys.Add(node.Text.LocKey);
                }
            });

            foreach (var key in usedKeys)
            {
                if (!reportedMissingKeys.Contains(key) && !HasDefaultTranslation(context, key))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "TXT002", $"Layout locKey '{key}' must exist in strings.json defaultCulture '{context.Strings.DefaultCulture}'.");
                }
            }

            foreach (var key in (context.Strings.Strings ?? new Dictionary<string, Dictionary<string, string>>()).Keys)
            {
                if (!usedKeys.Contains(key))
                {
                    context.Add(KKUIPipelineIssueSeverity.Warning, "TXT005", $"String key '{key}' is declared but not referenced by layout locKey.");
                }
            }

            foreach (var culture in context.Strings.Cultures ?? new List<string>())
            {
                if (culture == context.Strings.DefaultCulture)
                {
                    continue;
                }

                var missingAny = (context.Strings.Strings ?? new Dictionary<string, Dictionary<string, string>>())
                    .Values
                    .Any(values => values == null || !values.ContainsKey(culture));
                if (missingAny)
                {
                    context.Add(KKUIPipelineIssueSeverity.Warning, "LOC010", $"Culture '{culture}' is missing one or more optional translations; runtime will fallback to '{context.Strings.DefaultCulture}'.");
                }
            }
        }

        private static void AddLayoutLocKeys(UiLayoutNode node, HashSet<string> usedKeys)
        {
            AddLocKey(usedKeys, node.Text?.LocKey);
            AddLocKey(usedKeys, node.Toggle?.LabelLocKey);
            AddLocKey(usedKeys, node.InputField?.TextLocKey);
            AddLocKey(usedKeys, node.InputField?.PlaceholderLocKey);
            foreach (var option in node.Dropdown?.Options ?? new List<UiDropdownOptionSpec>())
            {
                AddLocKey(usedKeys, option.LocKey);
            }
        }

        private static void AddLocKey(HashSet<string> usedKeys, string locKey)
        {
            if (!string.IsNullOrWhiteSpace(locKey))
            {
                usedKeys.Add(locKey);
            }
        }

        private static bool HasDefaultTranslation(KKUIPipelineContext context, string locKey)
        {
            if (string.IsNullOrWhiteSpace(locKey) ||
                string.IsNullOrWhiteSpace(context.Strings.DefaultCulture) ||
                context.Strings.Strings == null ||
                !context.Strings.Strings.TryGetValue(locKey, out var values) ||
                values == null)
            {
                return false;
            }

            return values.ContainsKey(context.Strings.DefaultCulture);
        }

        private static void ValidateStringManifest(KKUIPipelineContext context)
        {
            if (context.Strings.SchemaVersion != "1.0")
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LOC001", "strings schemaVersion must be 1.0.");
            }

            if (string.IsNullOrWhiteSpace(context.Strings.DefaultCulture))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LOC002", "strings defaultCulture is required.");
            }

            if (context.Strings.Cultures == null || !context.Strings.Cultures.Contains(context.Strings.DefaultCulture))
            {
                context.Add(KKUIPipelineIssueSeverity.Error, "LOC003", "strings cultures must contain defaultCulture.");
            }

            foreach (var entry in context.Strings.Strings ?? new Dictionary<string, Dictionary<string, string>>())
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || !KeyRegex.IsMatch(entry.Key))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "LOC011", $"String key '{entry.Key}' must use lowercase letters, digits, underscores, or dots.");
                }

                if (entry.Value == null || !entry.Value.ContainsKey(context.Strings.DefaultCulture))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "LOC005", $"String key '{entry.Key}' must define defaultCulture '{context.Strings.DefaultCulture}'.");
                }
            }

            var constantNames = new HashSet<string>();
            foreach (var key in (context.Strings.Strings ?? new Dictionary<string, Dictionary<string, string>>()).Keys)
            {
                var constantName = ToPascal(key);
                if (!constantNames.Add(constantName))
                {
                    context.Add(KKUIPipelineIssueSeverity.Error, "LOC012", $"Generated string constant name '{constantName}' is duplicated.");
                }
            }
        }

        private static string ToPascal(string value)
        {
            return string.Concat(value
                .Split('_', '-', '.', ' ')
                .Where(part => part.Length > 0)
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }
    }
}
