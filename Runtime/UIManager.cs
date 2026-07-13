using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly Dictionary<string, Task<UIViewBase>> _showingTasks = new Dictionary<string, Task<UIViewBase>>();
        private readonly Dictionary<string, Task> _hidingTasks = new Dictionary<string, Task>();
        private readonly Dictionary<string, Task> _closingTasks = new Dictionary<string, Task>();
        private readonly Dictionary<string, SemaphoreSlim> _operationGates = new Dictionary<string, SemaphoreSlim>();
        private readonly Dictionary<string, Func<UIControllerBase>> _controllerFactories = new Dictionary<string, Func<UIControllerBase>>();
        private readonly List<UIBusRoute> _busRoutes = new List<UIBusRoute>();
        private readonly AddressablesUILoader _loader = new AddressablesUILoader(GetAddressablesKey);
        private readonly UIPanelCache _panels = new UIPanelCache();
        private readonly UILayerManager _layers = new UILayerManager();
        private readonly UIServiceRegistry _services = new UIServiceRegistry();

        [SerializeField] private string _startupCulture = "zh-Hans";
        [SerializeField, Min(0.01f)] private float _transitionTimeoutSeconds = 5f;
        private RectTransform _uiRoot;
        private UIBusRouter _busRouter;
        private bool _isDestroying;

        internal float TransitionTimeoutSeconds
        {
            get => _transitionTimeoutSeconds;
            set => _transitionTimeoutSeconds = Mathf.Max(0.01f, value);
        }

        internal Func<string, Task<GameObject>> PrefabLoaderOverride { get; set; }
        internal Action<string> PrefabReleaseOverride { get; set; }
        internal Func<string, UIControllerBase> ControllerFactoryOverride { get; set; }

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
            _isDestroying = true;
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
            EnsureSystemId(systemId);
            if (_panels.TryGetActive(systemId, out _) || _panels.TryGetHidden(systemId, out _))
            {
                return Task.CompletedTask;
            }

            return LoadPrefabAsync(systemId);
        }

        public Task<UIViewBase> OpenAsync(string systemId, MessagePayload payload = null)
        {
            EnsureSystemId(systemId);
            return StartTrackedViewOperation(
                _openingTasks,
                systemId,
                () => RunSerializedAsync(systemId, () => OpenOrShowCoreAsync(systemId, payload)));
        }

        public Task<UIViewBase> ShowAsync(string systemId, MessagePayload payload = null)
        {
            EnsureSystemId(systemId);
            return StartTrackedViewOperation(
                _showingTasks,
                systemId,
                () => RunSerializedAsync(systemId, () => OpenOrShowCoreAsync(systemId, payload)));
        }

        public Task HideAsync(string systemId)
        {
            EnsureSystemId(systemId);
            var acceptedWhileQueued = IsQueuedTopFirstRequestAccepted(systemId);
            return StartTrackedOperation(
                _hidingTasks,
                systemId,
                () => RunSerializedAsync(systemId, () => HideCoreAsync(systemId, acceptedWhileQueued)));
        }

        public Task CloseAsync(string systemId)
        {
            return CloseAsync(systemId, UICloseMode.Destroy);
        }

        public Task CloseAsync(string systemId, UICloseMode mode)
        {
            EnsureSystemId(systemId);
            if (mode == UICloseMode.Hide)
            {
                return HideAsync(systemId);
            }

            var acceptedWhileQueued = IsQueuedTopFirstRequestAccepted(systemId);
            return StartTrackedOperation(
                _closingTasks,
                systemId,
                () => RunSerializedAsync(systemId, () => CloseCoreAsync(systemId, acceptedWhileQueued)));
        }

        public Task ReleaseAsync(string systemId)
        {
            return CloseAsync(systemId, UICloseMode.Destroy);
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

        private async Task<UIViewBase> OpenOrShowCoreAsync(string systemId, MessagePayload payload)
        {
            if (_panels.TryGetActive(systemId, out var active))
            {
                return active.View;
            }

            if (_panels.TryGetHidden(systemId, out _))
            {
                return await ShowCoreAsync(systemId, payload);
            }

            return await OpenCoreAsync(systemId, payload);
        }

        private async Task<UIViewBase> OpenCoreAsync(string systemId, MessagePayload payload)
        {
            _states[systemId] = UIState.Loading;
            var managerCancellation = destroyCancellationToken;
            GameObject instance = null;
            UIControllerBase controller = null;
            UIPanelInstance panel = null;
            var pushedToLayerStack = false;

            try
            {
                var prefab = await LoadPrefabAsync(systemId);
                managerCancellation.ThrowIfCancellationRequested();
                if (prefab == null)
                {
                    throw new InvalidOperationException($"UI prefab loader returned null for '{systemId}'.");
                }

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

                panel = new UIPanelInstance(systemId, instance, view, controller);
                _panels.AddActive(panel);
                controller.OnPreOpen();
                controller.Initialize(payload);
                controller.Flush();

                instance.SetActive(true);
                var previousTop = _layers.Top;
                if (previousTop.view != null && _panels.TryGetActive(previousTop.systemId, out var previousPanel))
                {
                    DeactivateIfActive(previousPanel, false);
                }

                _layers.Push(systemId, view);
                pushedToLayerStack = true;
                view.SetInteraction(false, true);
                controller.OnOpened();
                await RunTransitionAsync(systemId, "Open", view, view.PlayOpenTransitionAsync);

                _states[systemId] = UIState.Open;
                ActivateIfReadyTop();
                return view;
            }
            catch
            {
                CleanupFailedPanel(systemId, panel, instance, controller, pushedToLayerStack);
                throw;
            }
        }

        private async Task<UIViewBase> ShowCoreAsync(string systemId, MessagePayload payload)
        {
            if (!_panels.MoveHiddenToActive(systemId, out var panel))
            {
                return await OpenCoreAsync(systemId, payload);
            }

            _states[systemId] = UIState.Showing;
            try
            {
                _layers.AssignSortingOrder(panel.Root);
                panel.Root.SetActive(true);
                var previousTop = _layers.Top;
                if (previousTop.view != null && _panels.TryGetActive(previousTop.systemId, out var previousPanel))
                {
                    DeactivateIfActive(previousPanel, false);
                }

                _layers.Push(systemId, panel.View);
                panel.View.SetInteraction(false, true);
                panel.Controller.OnShown(payload);
                await RunTransitionAsync(systemId, "Show", panel.View, panel.View.PlayShowTransitionAsync);

                _states[systemId] = UIState.Open;
                ActivateIfReadyTop();
                return panel.View;
            }
            catch (TimeoutException)
            {
                RollbackShow(systemId, panel, true);
                throw;
            }
            catch (OperationCanceledException)
            {
                CleanupCancelledPanel(systemId, panel);
                throw;
            }
            catch
            {
                RollbackShow(systemId, panel, true);
                throw;
            }
        }

        private async Task HideCoreAsync(string systemId, bool acceptedWhileQueued)
        {
            if (GetState(systemId) != UIState.Open)
            {
                return;
            }

            if (!_layers.IsTop(systemId) && !acceptedWhileQueued)
            {
                Debug.LogWarning($"[UIManager] HideAsync('{systemId}') ignored: not top layer. Hide top-first.");
                return;
            }

            if (!_panels.TryGetActive(systemId, out var panel))
            {
                _states[systemId] = UIState.Unloaded;
                return;
            }

            _states[systemId] = UIState.Hiding;
            try
            {
                DeactivateIfActive(panel, _layers.IsTop(systemId));
                await RunTransitionAsync(systemId, "Hide", panel.View, panel.View.PlayHideTransitionAsync);
            }
            catch (TimeoutException)
            {
                RollbackToOpen(systemId);
                throw;
            }
            catch (OperationCanceledException)
            {
                CleanupCancelledPanel(systemId, panel);
                throw;
            }
            catch
            {
                RollbackToOpen(systemId);
                throw;
            }

            _layers.Remove(systemId);
            _panels.MoveActiveToHidden(systemId, out _);
            panel.Root.SetActive(false);
            _states[systemId] = UIState.Hidden;
            try
            {
                panel.Controller.OnHidden();
            }
            finally
            {
                ActivateIfReadyTop();
            }
        }

        private async Task CloseCoreAsync(string systemId, bool acceptedWhileQueued)
        {
            if (GetState(systemId) == UIState.Hidden)
            {
                if (_panels.RemoveHidden(systemId, out var hidden))
                {
                    ReleaseHiddenPanel(systemId, hidden);
                }
                else
                {
                    ReleasePrefab(systemId);
                    _states[systemId] = UIState.Unloaded;
                }

                return;
            }

            if (GetState(systemId) != UIState.Open)
            {
                if (GetState(systemId) == UIState.Unloaded)
                {
                    ReleasePrefab(systemId);
                }

                return;
            }

            if (!_layers.IsTop(systemId) && !acceptedWhileQueued)
            {
                Debug.LogWarning($"[UIManager] CloseAsync('{systemId}') ignored: not top layer. Close top-first.");
                return;
            }

            if (!_panels.TryGetActive(systemId, out var panel))
            {
                _states[systemId] = UIState.Unloaded;
                return;
            }

            _states[systemId] = UIState.Closing;
            try
            {
                DeactivateIfActive(panel, _layers.IsTop(systemId));
                panel.Controller.OnPreClose();
                await RunTransitionAsync(systemId, "Close", panel.View, panel.View.PlayCloseTransitionAsync);
            }
            catch (TimeoutException)
            {
                RollbackToOpen(systemId);
                throw;
            }
            catch (OperationCanceledException)
            {
                CleanupCancelledPanel(systemId, panel);
                throw;
            }
            catch
            {
                RollbackToOpen(systemId);
                throw;
            }

            CommitActiveClose(systemId, panel);
        }

        internal async Task RunTransitionAsync(
            string systemId,
            string phase,
            UIViewBase view,
            Func<CancellationToken, Task> transition)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (transition == null)
            {
                throw new ArgumentNullException(nameof(transition));
            }

            var managerCancellation = destroyCancellationToken;
            var viewCancellation = view.destroyCancellationToken;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(managerCancellation, viewCancellation);
            using var timeoutCancellation = new CancellationTokenSource();

            Task transitionTask;
            try
            {
                transitionTask = transition(linked.Token) ?? Task.CompletedTask;
            }
            catch (OperationCanceledException) when (managerCancellation.IsCancellationRequested || viewCancellation.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogTransitionException(systemId, phase, view, ex);
                return;
            }

            var timeout = TimeSpan.FromSeconds(Mathf.Max(0.01f, _transitionTimeoutSeconds));
            var timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);
            var destroyedTask = Task.Delay(Timeout.Infinite, linked.Token);
            var completed = await Task.WhenAny(transitionTask, timeoutTask, destroyedTask);

            if (completed == transitionTask)
            {
                timeoutCancellation.Cancel();
                try
                {
                    await transitionTask;
                }
                catch (OperationCanceledException) when (managerCancellation.IsCancellationRequested || viewCancellation.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogTransitionException(systemId, phase, view, ex);
                }

                return;
            }

            timeoutCancellation.Cancel();
            if (managerCancellation.IsCancellationRequested || viewCancellation.IsCancellationRequested || completed == destroyedTask)
            {
                ObserveLateFault(transitionTask);
                var cancellation = viewCancellation.IsCancellationRequested ? viewCancellation : managerCancellation;
                throw new OperationCanceledException($"UI transition '{systemId}/{phase}' was cancelled because its owner was destroyed.", cancellation);
            }

            CancelWithoutMasking(linked);
            ObserveLateFault(transitionTask);
            throw new TimeoutException($"UI transition '{systemId}/{phase}' on '{view.GetType().Name}' exceeded {_transitionTimeoutSeconds:0.###} seconds.");
        }

        private void DeactivateIfActive(UIPanelInstance panel, bool blocksRaycasts)
        {
            panel.View.SetInteraction(false, blocksRaycasts);
            if (!panel.IsActivated)
            {
                return;
            }

            panel.IsActivated = false;
            panel.Controller.OnDeactivated();
        }

        private void ActivateIfReadyTop()
        {
            var top = _layers.Top;
            if (top.view == null || !_panels.TryGetActive(top.systemId, out var panel))
            {
                return;
            }

            if (GetState(top.systemId) != UIState.Open)
            {
                panel.View.SetInteraction(false, true);
                return;
            }

            panel.View.SetInteraction(true, true);
            if (panel.IsActivated)
            {
                return;
            }

            panel.IsActivated = true;
            panel.Controller.OnActivated();
        }

        private void RollbackShow(string systemId, UIPanelInstance panel, bool callHidden)
        {
            _layers.Remove(systemId);
            _panels.MoveActiveToHidden(systemId, out _);
            if (panel.Root != null)
            {
                panel.Root.SetActive(false);
            }

            _states[systemId] = UIState.Hidden;
            if (callHidden)
            {
                TryLifecycle(panel.Controller.OnHidden);
            }

            TryActivateTop();
        }

        private void RollbackToOpen(string systemId)
        {
            _states[systemId] = UIState.Open;
            TryActivateTop();
        }

        private void CleanupFailedPanel(
            string systemId,
            UIPanelInstance panel,
            GameObject instance,
            UIControllerBase controller,
            bool pushedToLayerStack)
        {
            if (pushedToLayerStack)
            {
                _layers.Remove(systemId);
            }

            _panels.Remove(systemId);
            _states[systemId] = UIState.Unloaded;
            TryDispose(controller ?? panel?.Controller);
            DestroyObject(instance ?? panel?.Root);
            ReleasePrefab(systemId);
            TryActivateTop();
        }

        private void CleanupCancelledPanel(string systemId, UIPanelInstance panel)
        {
            _layers.Remove(systemId);
            _panels.Remove(systemId);
            _states[systemId] = UIState.Unloaded;
            TryDispose(panel?.Controller);
            DestroyObject(panel?.Root);
            ReleasePrefab(systemId);
            TryActivateTop();
        }

        private void CommitActiveClose(string systemId, UIPanelInstance panel)
        {
            Exception firstException = null;
            CaptureException(panel.Controller.Close, ref firstException);
            _panels.RemoveActive(systemId, out _);
            _layers.Remove(systemId);
            CaptureException(panel.Controller.OnClosed, ref firstException);
            CaptureException(panel.Controller.Dispose, ref firstException);
            DestroyObject(panel.Root);
            ReleasePrefab(systemId);
            _states[systemId] = UIState.Unloaded;
            if (!_isDestroying)
            {
                CaptureException(ActivateIfReadyTop, ref firstException);
            }

            if (firstException != null)
            {
                throw firstException;
            }
        }

        private void ReleaseHiddenPanel(string systemId, UIPanelInstance panel)
        {
            Exception firstException = null;
            CaptureException(panel.Controller.OnPreClose, ref firstException);
            CaptureException(panel.Controller.Close, ref firstException);
            CaptureException(panel.Controller.OnClosed, ref firstException);
            CaptureException(panel.Controller.Dispose, ref firstException);
            DestroyObject(panel.Root);
            ReleasePrefab(systemId);
            _states[systemId] = UIState.Unloaded;

            if (firstException != null)
            {
                throw firstException;
            }
        }

        private Task<T> RunSerializedAsync<T>(string systemId, Func<Task<T>> operation)
        {
            return RunSerializedCoreAsync(systemId, operation);
        }

        private Task RunSerializedAsync(string systemId, Func<Task> operation)
        {
            return RunSerializedCoreAsync(systemId, operation);
        }

        private async Task<T> RunSerializedCoreAsync<T>(string systemId, Func<Task<T>> operation)
        {
            var managerCancellation = destroyCancellationToken;
            var gate = GetOperationGate(systemId);
            await gate.WaitAsync(managerCancellation);
            try
            {
                managerCancellation.ThrowIfCancellationRequested();
                if (_isDestroying)
                {
                    throw new OperationCanceledException(managerCancellation);
                }

                return await operation();
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task RunSerializedCoreAsync(string systemId, Func<Task> operation)
        {
            var managerCancellation = destroyCancellationToken;
            var gate = GetOperationGate(systemId);
            await gate.WaitAsync(managerCancellation);
            try
            {
                managerCancellation.ThrowIfCancellationRequested();
                if (_isDestroying)
                {
                    throw new OperationCanceledException(managerCancellation);
                }

                await operation();
            }
            finally
            {
                gate.Release();
            }
        }

        private Task<UIViewBase> StartTrackedViewOperation(
            Dictionary<string, Task<UIViewBase>> tasks,
            string systemId,
            Func<Task<UIViewBase>> operation)
        {
            if (tasks.TryGetValue(systemId, out var existing))
            {
                return existing;
            }

            var task = operation();
            tasks[systemId] = task;
            _ = RemoveTrackedViewOperationAsync(tasks, systemId, task);
            return task;
        }

        private Task StartTrackedOperation(
            Dictionary<string, Task> tasks,
            string systemId,
            Func<Task> operation)
        {
            if (tasks.TryGetValue(systemId, out var existing))
            {
                return existing;
            }

            var task = operation();
            tasks[systemId] = task;
            _ = RemoveTrackedOperationAsync(tasks, systemId, task);
            return task;
        }

        private static async Task RemoveTrackedViewOperationAsync(
            Dictionary<string, Task<UIViewBase>> tasks,
            string systemId,
            Task<UIViewBase> task)
        {
            try
            {
                await task;
            }
            catch
            {
            }
            finally
            {
                if (tasks.TryGetValue(systemId, out var current) && ReferenceEquals(current, task))
                {
                    tasks.Remove(systemId);
                }
            }
        }

        private static async Task RemoveTrackedOperationAsync(
            Dictionary<string, Task> tasks,
            string systemId,
            Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }
            finally
            {
                if (tasks.TryGetValue(systemId, out var current) && ReferenceEquals(current, task))
                {
                    tasks.Remove(systemId);
                }
            }
        }

        private SemaphoreSlim GetOperationGate(string systemId)
        {
            if (!_operationGates.TryGetValue(systemId, out var gate))
            {
                gate = new SemaphoreSlim(1, 1);
                _operationGates[systemId] = gate;
            }

            return gate;
        }

        private bool IsQueuedTopFirstRequestAccepted(string systemId)
        {
            var state = GetState(systemId);
            return _layers.IsTop(systemId) || state == UIState.Loading;
        }

        private Task<GameObject> LoadPrefabAsync(string systemId)
        {
            return PrefabLoaderOverride != null
                ? PrefabLoaderOverride(systemId)
                : _loader.LoadPrefabAsync(systemId);
        }

        private void ReleasePrefab(string systemId)
        {
            if (PrefabLoaderOverride != null)
            {
                PrefabReleaseOverride?.Invoke(systemId);
                return;
            }

            _loader.Release(systemId);
        }

        private UIControllerBase CreateController(string systemId)
        {
            if (ControllerFactoryOverride != null)
            {
                return ControllerFactoryOverride(systemId)
                    ?? throw new InvalidOperationException($"UI controller factory override returned null for '{systemId}'.");
            }

            if (!_controllerFactories.TryGetValue(systemId, out var factory))
            {
                throw new InvalidOperationException($"No UI controller factory registered for '{systemId}'. Ensure generated UI registration code has compiled and loaded.");
            }

            return factory();
        }

        private static void CaptureException(Action action, ref Exception firstException)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (firstException == null)
                {
                    firstException = ex;
                }
            }
        }

        private static void TryLifecycle(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void TryDispose(UIControllerBase controller)
        {
            if (controller == null)
            {
                return;
            }

            try
            {
                controller.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void TryActivateTop()
        {
            if (_isDestroying)
            {
                return;
            }

            try
            {
                ActivateIfReadyTop();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void LogTransitionException(string systemId, string phase, UIViewBase view, Exception exception)
        {
            Debug.LogError($"[UIManager] {systemId} {phase} transition on {view.GetType().Name} failed and will continue to the target state.\n{exception}");
        }

        private static void CancelWithoutMasking(CancellationTokenSource cancellation)
        {
            try
            {
                cancellation.Cancel();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void ObserveLateFault(Task task)
        {
            if (task == null)
            {
                return;
            }

            if (task.IsFaulted)
            {
                _ = task.Exception;
                return;
            }

            if (!task.IsCompleted)
            {
                _ = task.ContinueWith(
                    completed => _ = completed.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
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

        private static void EnsureSystemId(string systemId)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                throw new ArgumentException("UI system id cannot be null or empty.", nameof(systemId));
            }
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
