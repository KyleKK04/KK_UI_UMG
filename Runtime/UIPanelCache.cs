using System.Collections.Generic;
using UnityEngine;

namespace KK.UI.UMG
{
    internal sealed class UIPanelCache
    {
        private readonly Dictionary<string, UIPanelInstance> _active = new Dictionary<string, UIPanelInstance>();
        private readonly Dictionary<string, UIPanelInstance> _hidden = new Dictionary<string, UIPanelInstance>();

        public bool TryGetActive(string systemId, out UIPanelInstance instance)
        {
            return _active.TryGetValue(systemId, out instance);
        }

        public bool TryGetHidden(string systemId, out UIPanelInstance instance)
        {
            return _hidden.TryGetValue(systemId, out instance);
        }

        public void AddActive(UIPanelInstance instance)
        {
            instance.IsHidden = false;
            _hidden.Remove(instance.SystemId);
            _active[instance.SystemId] = instance;
        }

        public bool MoveActiveToHidden(string systemId, out UIPanelInstance instance)
        {
            if (!_active.TryGetValue(systemId, out instance))
            {
                return false;
            }

            _active.Remove(systemId);
            instance.IsHidden = true;
            _hidden[systemId] = instance;
            return true;
        }

        public bool MoveHiddenToActive(string systemId, out UIPanelInstance instance)
        {
            if (!_hidden.TryGetValue(systemId, out instance))
            {
                return false;
            }

            _hidden.Remove(systemId);
            instance.IsHidden = false;
            _active[systemId] = instance;
            return true;
        }

        public bool RemoveActive(string systemId, out UIPanelInstance instance)
        {
            if (!_active.TryGetValue(systemId, out instance))
            {
                return false;
            }

            _active.Remove(systemId);
            return true;
        }

        public bool RemoveHidden(string systemId, out UIPanelInstance instance)
        {
            if (!_hidden.TryGetValue(systemId, out instance))
            {
                return false;
            }

            _hidden.Remove(systemId);
            return true;
        }

        public void Remove(string systemId)
        {
            _active.Remove(systemId);
            _hidden.Remove(systemId);
        }

        public void DestroyAll()
        {
            DestroyInstances(_active.Values);
            DestroyInstances(_hidden.Values);
            _active.Clear();
            _hidden.Clear();
        }

        private static void DestroyInstances(IEnumerable<UIPanelInstance> instances)
        {
            foreach (var instance in instances)
            {
                if (instance?.Root == null)
                {
                    continue;
                }

                try
                {
                    instance.Controller?.Dispose();
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(instance.Root);
                }
                else
                {
                    Object.DestroyImmediate(instance.Root);
                }
            }
        }
    }
}
