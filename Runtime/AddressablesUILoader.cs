using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace KK.UI.UMG
{
    internal sealed class AddressablesUILoader
    {
        private readonly Func<string, string> _keyResolver;
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private readonly Dictionary<string, Task<GameObject>> _loadingTasks = new Dictionary<string, Task<GameObject>>();

        public AddressablesUILoader(Func<string, string> keyResolver)
        {
            _keyResolver = keyResolver ?? throw new ArgumentNullException(nameof(keyResolver));
        }

        public Task PreloadAsync(string systemId)
        {
            return LoadPrefabAsync(systemId);
        }

        public Task<GameObject> LoadPrefabAsync(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("UI system id cannot be null or empty.", nameof(systemId));
            }

            if (_handles.TryGetValue(systemId, out var existing) && existing.IsValid())
            {
                if (existing.Status == AsyncOperationStatus.Succeeded)
                {
                    return Task.FromResult(existing.Result);
                }

                return AwaitHandleAsync(systemId, existing);
            }

            if (_loadingTasks.TryGetValue(systemId, out var loadingTask))
            {
                return loadingTask;
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(_keyResolver(systemId));
            _handles[systemId] = handle;
            var task = AwaitHandleAsync(systemId, handle);
            _loadingTasks[systemId] = task;
            return task;
        }

        public void Release(string systemId)
        {
            _loadingTasks.Remove(systemId);
            if (!_handles.TryGetValue(systemId, out var handle))
            {
                return;
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            _handles.Remove(systemId);
        }

        public void ReleaseAll()
        {
            foreach (var handle in _handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _handles.Clear();
            _loadingTasks.Clear();
        }

        private async Task<GameObject> AwaitHandleAsync(string systemId, AsyncOperationHandle<GameObject> handle)
        {
            try
            {
                await handle.Task;
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }

                var exception = handle.OperationException ?? new InvalidOperationException($"Addressables load failed for UI '{systemId}'.");
                Release(systemId);
                throw exception;
            }
            finally
            {
                _loadingTasks.Remove(systemId);
            }
        }
    }
}
