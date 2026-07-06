using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KK.UI.UMG.Localization;

namespace KK.UI.UMG.Tests
{
    public sealed class UILocalizationServiceTests
    {
        [Test]
        public void ResolveUsesCurrentCulture()
        {
            var systemId = UniqueSystemId();
            UILocalizationService.Instance.SetStartupCulture("en-US");
            UILocalizationService.Instance.RegisterTable(systemId, CreateTable(systemId));

            var result = UILocalizationService.Instance.Resolve(systemId, "message");

            Assert.That(result, Is.EqualTo("Hello"));
        }

        [Test]
        public void ResolveFallsBackToDefaultCulture()
        {
            var systemId = UniqueSystemId();
            UILocalizationService.Instance.SetStartupCulture("ja-JP");
            UILocalizationService.Instance.RegisterTable(systemId, CreateTable(systemId));

            var result = UILocalizationService.Instance.Resolve(systemId, "message");

            Assert.That(result, Is.EqualTo("你好"));
        }

        [Test]
        public void MissingKeyReturnsKeyAndLogsWarning()
        {
            var systemId = UniqueSystemId();
            UILocalizationService.Instance.RegisterTable(systemId, CreateTable(systemId));

            LogAssert.Expect(LogType.Warning, $"[UILocalization] Missing key 'missing' in table '{systemId}'.");
            var result = UILocalizationService.Instance.Resolve(systemId, "missing");

            Assert.That(result, Is.EqualTo("missing"));
        }

        [Test]
        public void MissingTableReturnsKeyAndLogsWarning()
        {
            var systemId = UniqueSystemId();

            LogAssert.Expect(LogType.Warning, $"[UILocalization] Missing string table for UI system '{systemId}'.");
            var result = UILocalizationService.Instance.Resolve(systemId, "message");

            Assert.That(result, Is.EqualTo("message"));
        }

        private static UILocalizedStringTable CreateTable(string systemId)
        {
            return new UILocalizedStringTable(
                systemId,
                "zh-Hans",
                new Dictionary<string, Dictionary<string, string>>
                {
                    ["message"] = new Dictionary<string, string>
                    {
                        ["zh-Hans"] = "你好",
                        ["en-US"] = "Hello"
                    }
                });
        }

        private static string UniqueSystemId()
        {
            return "LocTest_" + Guid.NewGuid().ToString("N");
        }
    }
}
