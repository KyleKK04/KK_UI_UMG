using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using KK.UI.UMG.Internal;
using KK.UI.UMG.Localization;
using KK.UI.UMG.MessageBus;

namespace KK.UI.UMG
{
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private readonly Dictionary<string, UIState> _states = new Dictionary<string, UIState>();
        private readonly Dictionary<string, UIViewBase> _activeViews = new Dictionary<string, UIViewBase>();
        private readonly Dictionary<string, UIControllerBase> _activeControllers = new Dictionary<string, UIControllerBase>();
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private readonly HashSet<string> _pendingClose = new HashSet<string>();
        private readonly LayerStack _layerStack = new LayerStack();
        private readonly Dictionary<string, Func<UIControllerBase>> _controllerFactories = new Dictionary<string, Func<UIControllerBase>>();
        private readonly List<UIBusRoute> _busRoutes = new List<UIBusRoute>();
        private readonly List<IDisposable> _busSubscriptions = new List<IDisposable>();
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        [SerializeField] private string _startupCulture = "zh-Hans";
        private int _nextSortingOrder;
        private RectTransform _uiRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            UILocalizationService.Instance.SetStartupCulture(_startupCulture);
            ControllerFactoryRegistry.CopyTo(_controllerFactories);
            UIBusRouteRegistry.CopyTo(_busRoutes);
            SubscribeBusRoutes();
            GetOrCreateUiRoot();
        }

        private void OnDestroy()
        {
            foreach (var subscription in _busSubscriptions)
            {
                subscription.Dispose();
            }

            _busSubscriptions.Clear();
            ClearServices();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public async Task<UIViewBase> OpenAsync(string systemId, MessagePayload payload = null)
        {
            if (IsOpen(systemId))
            {
                return _activeViews[systemId];
            }

            var state = GetState(systemId);
            if (state == UIState.Loading || state == UIState.Closing)
            {
                while (GetState(systemId) == UIState.Loading || GetState(systemId) == UIState.Closing)
                {
                    await Task.Yield();
                }

                return IsOpen(systemId) ? _activeViews[systemId] : null;
            }

            _states[systemId] = UIState.Loading;
            var handle = Addressables.LoadAssetAsync<GameObject>(GetAddressablesKey(systemId));
            _handles[systemId] = handle;

            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var operationException = handle.OperationException;
                _states[systemId] = UIState.Unloaded;
                Addressables.Release(handle);
                _handles.Remove(systemId);
                throw operationException;
            }

            var instance = Instantiate(handle.Result, GetOrCreateUiRoot());
            instance.SetActive(false);
            AssignSortingOrder(instance);
            var view = instance.GetComponent<UIViewBase>();
            if (view == null)
            {
                Destroy(instance);
                _states[systemId] = UIState.Unloaded;
                Addressables.Release(handle);
                _handles.Remove(systemId);
                throw new MissingComponentException($"UI prefab '{systemId}' must have UIViewBase.");
            }

            UIControllerBase controller;
            try
            {
                controller = CreateController(systemId);
            }
            catch
            {
                Destroy(instance);
                _states[systemId] = UIState.Unloaded;
                Addressables.Release(handle);
                _handles.Remove(systemId);
                throw;
            }

            controller.SystemId = systemId;
            controller.UIManager = this;
            var pushedToLayerStack = false;
            try
            {
                controller.BindView(view);
                _activeViews[systemId] = view;
                _activeControllers[systemId] = controller;
                _handles[systemId] = handle;
                controller.OnPreOpen();
                controller.Initialize(payload);
                controller.Flush();
                controller.OnOpened();

                instance.SetActive(true);
                var previousTop = _layerStack.Top;
                _layerStack.Push(systemId, view);
                pushedToLayerStack = true;
                if (previousTop.view != null && _activeControllers.TryGetValue(previousTop.systemId, out var previousController))
                {
                    previousController.OnDeactivated();
                }

                controller.OnActivated();
                _states[systemId] = UIState.Open;
                if (_pendingClose.Remove(systemId))
                {
                    await CloseAsync(systemId);
                    return null;
                }

                return view;
            }
            catch
            {
                if (pushedToLayerStack)
                {
                    _layerStack.Pop(systemId);
                }

                _activeControllers.Remove(systemId);
                _activeViews.Remove(systemId);
                _handles.Remove(systemId);
                _states[systemId] = UIState.Unloaded;
                controller.Dispose();
                Destroy(instance);
                Addressables.Release(handle);
                throw;
            }
        }

        public async Task CloseAsync(string systemId)
        {
            var state = GetState(systemId);
            if (state == UIState.Loading)
            {
                _pendingClose.Add(systemId);
                return;
            }

            if (state != UIState.Open)
            {
                return;
            }

            var top = _layerStack.Top;
            if (top.systemId != systemId)
            {
                Debug.LogWarning($"[UIManager] CloseAsync('{systemId}') ignored: not top layer. Close top-first.");
                return;
            }

            _states[systemId] = UIState.Closing;
            var view = _activeViews[systemId];
            _activeControllers.TryGetValue(systemId, out var controller);
            Exception closeException = null;
            try
            {
                controller?.OnPreClose();
                controller?.Close();
            }
            catch (Exception ex)
            {
                closeException = ex;
            }
            finally
            {
                _layerStack.Pop(systemId);
                try
                {
                    controller?.OnDeactivated();
                    var newTop = _layerStack.Top;
                    if (newTop.view != null && _activeControllers.TryGetValue(newTop.systemId, out var newTopController))
                    {
                        newTopController.OnActivated();
                    }

                    controller?.OnClosed();
                }
                catch (Exception ex)
                {
                    if (closeException == null)
                    {
                        closeException = ex;
                    }
                }

                try
                {
                    controller?.Dispose();
                }
                catch (Exception ex)
                {
                    if (closeException == null)
                    {
                        closeException = ex;
                    }
                }

                _activeControllers.Remove(systemId);
                Destroy(view.gameObject);
                _activeViews.Remove(systemId);

                if (_handles.TryGetValue(systemId, out var handle))
                {
                    Addressables.Release(handle);
                    _handles.Remove(systemId);
                }
            }

            await Task.Yield();
            _states[systemId] = UIState.Unloaded;
            if (closeException != null)
            {
                throw closeException;
            }
        }

        public bool IsOpen(string systemId)
        {
            return GetState(systemId) == UIState.Open && _activeViews.ContainsKey(systemId);
        }

        public UIState GetState(string systemId)
        {
            return _states.TryGetValue(systemId, out var state) ? state : UIState.Unloaded;
        }

        public UIViewBase GetTopLayer()
        {
            return _layerStack.Top.view;
        }

        public IReadOnlyList<string> GetLayerStack()
        {
            return _layerStack.Stack;
        }

        public void RegisterService<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var serviceType = typeof(T);
            if (_services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"Service '{serviceType.FullName}' is already registered. Unregister it before registering a replacement.");
            }

            _services.Add(serviceType, service);
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var raw) && raw is T typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public void UnregisterService<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        public void ClearServices()
        {
            _services.Clear();
        }

        private UIControllerBase CreateController(string systemId)
        {
            if (!_controllerFactories.TryGetValue(systemId, out var factory))
            {
                throw new InvalidOperationException($"No UI controller factory registered for '{systemId}'. Ensure generated UI registration code has compiled and loaded.");
            }

            return factory();
        }

        private void SubscribeBusRoutes()
        {
            foreach (var route in _busRoutes)
            {
                var routeCopy = route;
                var subscription = UIMessageBus.Subscribe(routeCopy.Channel, (channel, payload) =>
                {
                    if (routeCopy.Action == UIBusRouteAction.Open)
                    {
                        _ = OpenAsync(routeCopy.SystemId, payload ?? new MessagePayload());
                        return;
                    }

                    if (routeCopy.Action == UIBusRouteAction.Close)
                    {
                        _ = CloseAsync(routeCopy.SystemId);
                    }
                });

                _busSubscriptions.Add(subscription);
            }
        }

        private static string GetAddressablesKey(string systemId)
        {
            return $"UI/{systemId}/{systemId}View";
        }

        private void AssignSortingOrder(GameObject instance)
        {
            var canvas = instance.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = instance.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = _nextSortingOrder++;

            if (instance.GetComponent<GraphicRaycaster>() == null)
            {
                instance.AddComponent<GraphicRaycaster>();
            }

            if (instance.GetComponent<CanvasGroup>() == null)
            {
                instance.AddComponent<CanvasGroup>();
            }
        }

        private Transform GetOrCreateUiRoot()
        {
            if (_uiRoot != null)
            {
                return _uiRoot;
            }

            var canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                var canvasObject = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                CreateEventSystem();
            }

            _uiRoot = canvas.GetComponent<RectTransform>();
            return _uiRoot;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            var inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                eventSystemObject.AddComponent(inputSystemModuleType);
                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            eventSystemObject.AddComponent<StandaloneInputModule>();
#else
            Debug.LogWarning("Created EventSystem without an input module because no supported Unity input backend is enabled.");
#endif
        }

    }

}
