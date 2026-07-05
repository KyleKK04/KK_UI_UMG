using System;
using System.Collections.Generic;
using System.Linq;
using KK.UI.UMG.MessageBus;

namespace KK.UI.UMG.Internal
{
    public readonly struct UIBusRoute
    {
        public UIBusRoute(string systemId, string channel, UIBusRouteAction action)
        {
            SystemId = systemId;
            Channel = channel;
            Action = action;
        }

        public string SystemId { get; }
        public string Channel { get; }
        public UIBusRouteAction Action { get; }
    }

    public static class UIBusRouteRegistry
    {
        private static readonly List<UIBusRoute> Routes = new List<UIBusRoute>();

        public static void Register(string systemId, string channel, UIBusRouteAction action)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("UI system id cannot be null or empty.", nameof(systemId));
            }

            UIMessageBus.ValidateChannel(channel);

            if (Routes.Any(route =>
                route.SystemId == systemId &&
                route.Channel == channel &&
                route.Action == action))
            {
                return;
            }

            Routes.Add(new UIBusRoute(systemId, channel, action));
        }

        public static void CopyTo(List<UIBusRoute> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Clear();
            target.AddRange(Routes);
        }
    }
}
