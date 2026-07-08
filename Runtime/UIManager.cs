using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private readonly Dictionary<string, Task<UIViewBase>> _openingTasks = new Dictionary<string, Task<UIViewBase>>();
        private readonly Dictionary<string, Task> _closingTasks = new Dictionary<string, Task>();
        private readonly HashSet<string> _pendingClose = new HashSet<string>();
        private readonly Dictionary<string, Func<UIControllerBase>> _controllerFactories = new Dictionary<string, Func<UIControllerBase>>();
        private readonly List<UIBusRoute> _busRoutes = new List<UIBusRoute>();
        private readonly AddressablesUILoader _loader = new AddressablesUILoader(GetAddressablesKey);
        private readonly UIPanelCache _panels = new UIPanelCache();
        private readonly UILayerManager _layers = new UILayerManager();
        private readonly UIServiceRegistry _services = new UIServiceRegistry();

        [SerializeField] private string _startupCulture = "zh-Hans";
        private RectTransform _uiRoot;
        private UIBusRouter _busRouter;

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
            _busRouter = new UIBusRouter(OpenAsync, CloseAsync);
            _busRouter.Subscribe(_busRoutes);
            GetOrCreateUiRoot();
        }

        private void OnDestroy()
        {
            _busRouter?.Dispose();
            _panels.DestroyAll();
            _loader.ReleaseAll();
            ClearServices();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public Task PreloadAsync(string systemId)
        {
            if (IsOpen(systemId) || GetState(systemId) == UIState.Hidden)
            {
                return Task.CompletedTask;
            }

            return _loader.PreloadAsync(systemId);
        }

        public Task<UIViewBase> OpenAsync(string systemId, MessagePayload payload = null)
        {
            if (_panels.TryGetActive(systemId, out var active))
            {
                return Task.FromResult(active.View);
            }

            if (_panels.TryGetHidden(systemId, out _))
            {
                return ShowAsync(systemId, payload);
            }

            if (_openingTasks.TryGetValue(systemId, out var openingTask))
            {
                return openingTask;
            }

            if (_closingTasks.TryGetValue(systemId, out var closingTask))
            {
                return OpenAfterCloseAsync(systemId, payload, closingTask);
            }

            var task = OpenCoreAsync(systemId, payload);
            _openingTasks[systemId] = task;
            return task;
        }

        public async Task<UIViewBase> ShowAsync(string systemId, MessagePayload payload = null)
        {
            if (_panels.TryGetActive(systemId, out var active))
            {
                return active.View;
            }

            if (_closingTasks.TryGetValue(systemId, out var closingTask))
            {
                await closingTask;
            }

            if (!_panels.MoveHiddenToActive(systemId, out var panel))
            {
                return await OpenAsync(systemId, payload);
            }

            var previousTop = _layers.Top;
            _layers.AssignSortingOrder(panel.Root);
            panel.Root.SetActive(true);
            _layers.Push(systemId, panel.View);
            if (previousTop.view != null && _panels.TryGetActive(previousTop.systemId, out var previousPanel))
            {
                previousPanel.Controller.OnDeactivated();
            }

            panel.Controller.OnShown(payload);
            panel.Controller.OnActivated();
            _states[systemId] = UIState.Open;
            return panel.View;
        }

        public async Task HideAsync(string systemId)
        {
            if (_openingTasks.TryGetValue(systemId, out var openingTask))
            {
                await openingTask;
            }

            if (GetState(systemId) != UIState.Open)
            {
                return;
            }

            var top = _layers.Top;
            if (top.systemId != systemId)
            {
                Debug.LogWarning($"[UIManager] HideAsync('{systemId}') ignored: not top layer. Hide top-first.");
                return;
            }

            if (!_panels.MoveActiveToHidden(systemId, out var panel))
            {
                _states[systemId] = UIState.Unloaded;
                return;
            }

            panel.Controller.OnDeactivated();
            _layers.Pop(systemId);
            ActivateTopController();
            panel.Root.SetActive(false);
            panel.Controller.OnHidden();
            _states[systemId] = UIState.Hidden;
        }

        public Task CloseAsync(string systemId)
        {
            return CloseAsync(systemId, UICloseMode.Destroy);
        }

        public async Task CloseAsync(string systemId, UICloseMode mode)
        {
            if (mode == UICloseMode.Hide)
            {
                await HideAsync(systemId);
                return;
            }

            if (GetState(systemId) == UIState.Loading)
            {
                _pendingClose.Add(systemId);
                return;
            }

            if (_closingTasks.TryGetValue(systemId, out var closingTask))
            {
                await closingTask;
                return;
            }

            var task = CloseCoreAsync(systemId);
            _closingTasks[systemId] = task;
            try
            {
                await task;
            }
            finally
            {
                _closingTasks.Remove(systemId);
            }
        }

        public async Task ReleaseAsync(string systemId)
        {
            if (GetState(systemId) == UIState.Open)
            {
                await CloseAsync(systemId);
                return;
            }

            if (_panels.RemoveHidden(systemId, out var hidden))
            {
                ReleasePanel(systemId, hidden, false);
                return;
            }

            _loader.Release(systemId);
            _states[systemId] = UIState.Unloaded;
        }

        public bool IsOpen(string systemId)
        {
            return GetState(systemId) == UIState.Open && _panels.TryGetActive(systemId, out _);
        }

        public UIState GetState(string systemId)
        {
            return _states.TryGetValue(systemId, out var state) ? state : UIState.Unloaded;
        }

        public UIViewBase GetTopLayer()
        {
            return _layers.Top.view;
        }

        public IReadOnlyList<string> GetLayerStack()
        {
            return _layers.Stack;
        }

        public void RegisterService<T>(T service) where T : class
        {
            _services.RegisterService(service);
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            return _services.TryGetService(out service);
        }

        public void UnregisterService<T>() where T : class
        {
            _services.UnregisterService<T>();
        }

        public void ClearServices()
        {
            _services.Clear();
        }

        private async Task<UIViewBase> OpenAfterCloseAsync(string systemId, MessagePayload payload, Task closingTask)
        {
            await closingTask;
            return await OpenAsync(systemId, payload);
        }

        private async Task<UIViewBase> OpenCoreAsync(string systemId, MessagePayload payload)
        {
            _states[systemId] = UIState.Loading;
            GameObject instance = null;
            UIControllerBase controller = null;
            var pushedToLayerStack = false;
            try
            {
                var prefab = await _loader.LoadPrefabAsync(systemId);
                instance = Instantiate(prefab, GetOrCreateUiRoot());
                instance.SetActive(false);
                _layers.AssignSortingOrder(instance);

                var view = instance.GetComponent<UIViewBase>();
                if (view == null)
                {
                    throw new MissingComponentException($"UI prefab '{systemId}' must have UIViewBase.");
                }

                controller = CreateController(systemId);
                controller.SystemId = systemId;
                controller.UIManager = this;
                controller.BindView(view);

                var panel = new UIPanelInstance(systemId, instance, view, controller);
                _panels.AddActive(panel);
                controller.OnPreOpen();
                controller.Initialize(payload);
                controller.Flush();

                instance.SetActive(true);
                var previousTop = _layers.Top;
                _layers.Push(systemId, view);
                pushedToLayerStack = true;
                if (previousTop.view != null && _panels.TryGetActive(previousTop.systemId, out var previousPanel))
                {
                    previousPanel.Controller.OnDeactivated();
                }

                controller.OnOpened();
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
                    _layers.Pop(systemId);
                }

                _panels.Remove(systemId);
                _states[systemId] = UIState.Unloaded;
                _pendingClose.Remove(systemId);
                try
                {
                    controller?.Dispose();
                }
                finally
                {
                    DestroyObject(instance);
                    _loader.Release(systemId);
                }

                throw;
            }
            finally
            {
                _openingTasks.Remove(systemId);
            }
        }

        private Task CloseCoreAsync(string systemId)
        {
            var state = GetState(systemId);
            if (state == UIState.Hidden)
            {
                if (_panels.RemoveHidden(systemId, out var hidden))
                {
                    ReleasePanel(systemId, hidden, false);
                }

                return Task.CompletedTask;
            }

            if (state != UIState.Open)
            {
                return Task.CompletedTask;
            }

            var top = _layers.Top;
            if (top.systemId != systemId)
            {
                Debug.LogWarning($"[UIManager] CloseAsync('{systemId}') ignored: not top layer. Close top-first.");
                return Task.CompletedTask;
            }

            if (!_panels.RemoveActive(systemId, out var panel))
            {
                _states[systemId] = UIState.Unloaded;
                return Task.CompletedTask;
            }

            _states[systemId] = UIState.Closing;
            Exception closeException = null;
            try
            {
                panel.Controller.OnPreClose();
                panel.Controller.Close();
            }
            catch (Exception ex)
            {
                closeException = ex;
            }
            finally
            {
                _layers.Pop(systemId);
                try
                {
                    panel.Controller.OnDeactivated();
                    ActivateTopController();
                    panel.Controller.OnClosed();
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
                    panel.Controller.Dispose();
                }
                catch (Exception ex)
                {
                    if (closeException == null)
                    {
                        closeException = ex;
                    }
                }

                DestroyObject(panel.Root);
                _loader.Release(systemId);
                _states[systemId] = UIState.Unloaded;
            }

            if (closeException != null)
            {
                return Task.FromException(closeException);
            }

            return Task.CompletedTask;
        }

        private void ReleasePanel(string systemId, UIPanelInstance panel, bool callDeactivated)
        {
            Exception firstException = null;
            try
            {
                panel.Controller.OnPreClose();
                panel.Controller.Close();
                if (callDeactivated)
                {
                    panel.Controller.OnDeactivated();
                }

                panel.Controller.OnClosed();
            }
            catch (Exception ex)
            {
                firstException = ex;
            }

            try
            {
                panel.Controller.Dispose();
            }
            catch (Exception ex)
            {
                if (firstException == null)
                {
                    firstException = ex;
                }
            }

            DestroyObject(panel.Root);
            _loader.Release(systemId);
            _states[systemId] = UIState.Unloaded;

            if (firstException != null)
            {
                throw firstException;
            }
        }

        private void ActivateTopController()
        {
            var top = _layers.Top;
            if (top.view != null && _panels.TryGetActive(top.systemId, out var topPanel))
            {
                topPanel.Controller.OnActivated();
            }
        }

        private UIControllerBase CreateController(string systemId)
        {
            if (!_controllerFactories.TryGetValue(systemId, out var factory))
            {
                throw new InvalidOperationException($"No UI controller factory registered for '{systemId}'. Ensure generated UI registration code has compiled and loaded.");
            }

            return factory();
        }

        private static string GetAddressablesKey(string systemId)
        {
            return $"UI/{systemId}/{systemId}View";
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

        private static void DestroyObject(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }
    }
}
