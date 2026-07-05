using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace KK.UI.UMG.Editor.Pipeline
{
    internal static class BusChannelUtility
    {
        private static readonly Regex EventRegex = new Regex(@"^[a-z0-9_]+$", RegexOptions.Compiled);

        public static string BuildFullChannel(string packageId, string relativeChannel)
        {
            return $"ui.{packageId}.{relativeChannel}";
        }

        public static bool IsValidRelativeChannel(string relativeChannel, string expectedDirection)
        {
            if (string.IsNullOrWhiteSpace(relativeChannel))
            {
                return false;
            }

            if (!relativeChannel.StartsWith(expectedDirection + ".", StringComparison.Ordinal))
            {
                return false;
            }

            var parts = relativeChannel.Split('.');
            return parts.Length == 2 && parts[0] == expectedDirection && EventRegex.IsMatch(parts[1]);
        }

        public static bool IsValidFullChannel(string packageId, string fullChannel, string expectedDirection)
        {
            var parts = fullChannel?.Split('.');
            return parts != null &&
                   parts.Length == 4 &&
                   parts[0] == "ui" &&
                   parts[1] == packageId &&
                   parts[2] == expectedDirection &&
                   EventRegex.IsMatch(parts[3]);
        }

        public static string ToConstantName(string systemId, string relativeChannel)
        {
            var parts = relativeChannel.Split('.');
            return ToPascal(systemId) + string.Concat(parts.Select(ToPascal));
        }

        public static string ToPascal(string value)
        {
            return string.Concat(value
                .Split('.', '_', '-', ' ')
                .Where(part => part.Length > 0)
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
        }
    }
}
