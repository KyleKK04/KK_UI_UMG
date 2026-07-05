using System.Collections.Generic;
using UnityEngine;

namespace KK.UI.UMG.Localization
{
    public sealed class UILocalizedStringTable
    {
        private readonly Dictionary<string, Dictionary<string, string>> _strings;

        public UILocalizedStringTable(
            string systemId,
            string defaultCulture,
            Dictionary<string, Dictionary<string, string>> strings)
        {
            SystemId = systemId;
            DefaultCulture = defaultCulture;
            _strings = strings ?? new Dictionary<string, Dictionary<string, string>>();
        }

        public string SystemId { get; }
        public string DefaultCulture { get; }

        public string Resolve(string key, string culture)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (!_strings.TryGetValue(key, out var localizedValues))
            {
                Debug.LogWarning($"[UILocalization] Missing key '{key}' in table '{SystemId}'.");
                return key;
            }

            if (!string.IsNullOrWhiteSpace(culture) &&
                localizedValues.TryGetValue(culture, out var localizedValue))
            {
                return localizedValue;
            }

            if (localizedValues.TryGetValue(DefaultCulture, out var fallbackValue))
            {
                return fallbackValue;
            }

            Debug.LogWarning($"[UILocalization] Missing default culture '{DefaultCulture}' for key '{key}' in table '{SystemId}'.");
            return key;
        }
    }
}
