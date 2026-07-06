using System.Collections.Generic;
using NUnit.Framework;
using KK.UI.UMG.Internal;
using KK.UI.UMG.MessageBus;

namespace KK.UI.UMG.Tests
{
    public sealed class UIBusRouteRegistryTests
    {
        [Test]
        public void CopyToCopiesRegisteredRoutes()
        {
            var channel = "ui.RouteBox.in.open_requested";
            UIBusRouteRegistry.Register("RouteBox", channel, UIBusRouteAction.Open);
            var target = new List<UIBusRoute>();

            UIBusRouteRegistry.CopyTo(target);

            Assert.That(target.Exists(route =>
                route.SystemId == "RouteBox" &&
                route.Channel == channel &&
                route.Action == UIBusRouteAction.Open), Is.True);
        }

        [Test]
        public void DuplicateRouteRegistrationIsIgnored()
        {
            var channel = "ui.RouteBox.in.close_requested";
            UIBusRouteRegistry.Register("RouteBox", channel, UIBusRouteAction.Close);
            UIBusRouteRegistry.Register("RouteBox", channel, UIBusRouteAction.Close);
            var target = new List<UIBusRoute>();

            UIBusRouteRegistry.CopyTo(target);

            Assert.That(target.FindAll(route =>
                route.SystemId == "RouteBox" &&
                route.Channel == channel &&
                route.Action == UIBusRouteAction.Close), Has.Count.EqualTo(1));
        }
    }
}
