using System;
using UnityEngine;
using KK.UI.UMG;

namespace KK.UI.UMG.Samples.Inventory
{
    public sealed class KkSampleInventoryBootstrap : MonoBehaviour
    {
        private const string SystemId = "KkSampleInventoryPanel";

        [SerializeField] private MonoBehaviour _inventoryServiceSource;
        [SerializeField] private bool _openOnStart = true;

        private UIManager _registeredManager;

        private async void Start()
        {
            var manager = UIManager.Instance;
            if (manager == null)
            {
                Debug.LogError("KkSampleInventoryBootstrap requires a UIManager in the scene.");
                return;
            }

            var service = ResolveInventoryService();
            if (service == null)
            {
                Debug.LogError("KkSampleInventoryBootstrap requires a MonoBehaviour that implements IInventoryService.");
                return;
            }

            manager.RegisterService<IInventoryService>(service);
            _registeredManager = manager;

            if (!_openOnStart)
            {
                return;
            }

            try
            {
                await manager.OpenAsync(SystemId);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnDestroy()
        {
            if (_registeredManager != null)
            {
                _registeredManager.UnregisterService<IInventoryService>();
                _registeredManager = null;
            }
        }

        private IInventoryService ResolveInventoryService()
        {
            if (_inventoryServiceSource is IInventoryService assigned)
            {
                return assigned;
            }

            foreach (var component in GetComponents<MonoBehaviour>())
            {
                if (component is IInventoryService service)
                {
                    return service;
                }
            }

            return null;
        }
    }
}
