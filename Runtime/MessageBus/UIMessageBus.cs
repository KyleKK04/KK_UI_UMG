using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

[assembly: InternalsVisibleTo("kk.ui-umg.Runtime.Tests")]

namespace KK.UI.UMG.MessageBus
{
    public static class UIMessageBus
    {
        private static readonly Dictionary<string, List<Action<string, MessagePayload>>> Handlers =
            new Dictionary<string, List<Action<string, MessagePayload>>>();
        private static readonly Regex EventSegmentRegex = new Regex(@"^[a-z0-9_]+$", RegexOptions.Compiled);
#if UNITY_INCLUDE_TESTS
        internal static Action<Exception> ExceptionLogger { get; set; } = Debug.LogException;
#else
        private static readonly Action<Exception> ExceptionLogger = Debug.LogException;
#endif

        public static IDisposable Subscribe(string channel, Action<string, MessagePayload> handler)
        {
            ValidateChannel(channel);
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (!Handlers.TryGetValue(channel, out var handlers))
            {
                handlers = new List<Action<string, MessagePayload>>();
                Handlers[channel] = handlers;
            }

            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }

            return new UIMessageSubscription(channel, handler, Unsubscribe);
        }

        public static void Publish(string channel, MessagePayload payload = null)
        {
            ValidateChannel(channel);
            if (!Handlers.TryGetValue(channel, out var handlers) || handlers.Count == 0)
            {
                return;
            }

            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    handler(channel, payload);
                }
                catch (Exception ex)
                {
                    ExceptionLogger?.Invoke(ex);
                }
            }
        }

        internal static void ValidateChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException("UI message channel cannot be null or empty.", nameof(channel));
            }

            var parts = channel.Split('.');
            if (parts.Length != 4 || parts.Any(string.IsNullOrWhiteSpace) || parts[0] != "ui")
            {
                throw new ArgumentException($"UI message channel '{channel}' must match 'ui.<packageId>.<direction>.<event>'.", nameof(channel));
            }

            if (parts[2] != "in" && parts[2] != "out")
            {
                throw new ArgumentException($"UI message channel '{channel}' direction must be 'in' or 'out'.", nameof(channel));
            }

            if (!EventSegmentRegex.IsMatch(parts[3]))
            {
                throw new ArgumentException($"UI message channel '{channel}' event segment must use lowercase letters, digits, or underscores.", nameof(channel));
            }
        }

        private static void Unsubscribe(string channel, Action<string, MessagePayload> handler)
        {
            if (!Handlers.TryGetValue(channel, out var handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                Handlers.Remove(channel);
            }
        }
    }
}
