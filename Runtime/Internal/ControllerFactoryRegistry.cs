using System;
using System.Collections.Generic;

namespace KK.UI.UMG.Internal
{
    public static class ControllerFactoryRegistry
    {
        private static readonly Dictionary<string, Func<UIControllerBase>> Factories = new Dictionary<string, Func<UIControllerBase>>();

        public static void Register(string systemId, Func<UIControllerBase> factory)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("UI system id cannot be null or empty.", nameof(systemId));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            Factories[systemId] = factory;
        }

        public static void CopyTo(Dictionary<string, Func<UIControllerBase>> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Clear();
            foreach (var factory in Factories)
            {
                target.Add(factory.Key, factory.Value);
            }
        }
    }
}
