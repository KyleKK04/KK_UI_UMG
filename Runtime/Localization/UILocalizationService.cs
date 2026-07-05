using System;
using System.Collections.Generic;
using UnityEngine;

namespace KK.UI.UMG.Localization
{
    public sealed class UILocalizationService
    {
        private readonly Dictionary<string, UILocalizedStringTable> _tables =
            new Dictionary<string, UILocalizedStringTable>();

        public static UILocalizationService Instance { get; private set; } = new UILocalizationService();

        public string CurrentCulture { get; private set; } = "zh-Hans";

        public void SetStartupCulture(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                return;
            }

            CurrentCulture = culture;
        }

        public void RegisterTable(string systemId, UILocalizedStringTable table)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("UI system id cannot be null or empty.", nameof(systemId));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            _tables[systemId] = table;
        }

        public string Resolve(string systemId, string key)
        {
            if (!_tables.TryGetValue(systemId, out var table))
            {
                Debug.LogWarning($"[UILocalization] Missing string table for UI system '{systemId}'.");
                return key;
            }

            return table.Resolve(key, CurrentCulture);
        }

        internal static void ResetForTest()
        {
            Instance = new UILocalizationService();
        }
    }
}
