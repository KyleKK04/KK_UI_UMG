# KK_UI_UMG Business Adapter Reference

Use this reference when connecting an existing gameplay/business class to a KK_UI_UMG Source package.

## Default Flow

1. Read the UI Source package first: `bindings.json`, `codegen.json`, `README.md`, and any handwritten Controller partial.
2. Read the existing business class named by the user, such as `PlayerController`.
3. Define a UI-facing `I<Feature>Service` contract with only the data, commands, and changed events needed by this UI.
4. Create one adapter MonoBehaviour in the business directory, for example `Assets/Scripts/Game/Player/UIAdapters/PlayerHudServiceAdapter.cs`.
5. The adapter references the business object, registers the service with `UIManager.RegisterService<I<Feature>Service>()` in `Start`, and unregisters on destroy.
6. Put or update the handwritten Controller partial at `<Generated Parent>/<PackageId>/<PackageId>Controller.cs`.
7. Update `codegen.requiredServices`. The Controller maps service snapshots into Store fields and flushes once per UI event or business notification.

## Controller Partial Location

Handwritten Controller partials are UI-layer code, not business adapter code. Use:

```text
<Generated Parent>/<PackageId>/<PackageId>Controller.cs
```

Example:

```text
Assets/UI/Generated/PlayerHud/PlayerHudController.cs
```

Do not create extra UI subfolders such as:

```text
<Generated Parent>/<PackageId>/Controllers/
<Generated Parent>/<PackageId>/Business/
<Generated Parent>/<PackageId>/Partial/
```

Do not put handwritten partials inside `<Generated Parent>/<PackageId>/Scripts/`. `Scripts/` is generator-owned and may be overwritten. The root `<Generated Parent>/<PackageId>/` folder is the stable UI package folder for the handwritten partial.

## Business API Rules

- Prefer public read-only getters and changed events on existing business classes.
- If missing, add the minimum public API/event needed for the adapter.
- Do not use reflection to read private fields.
- Do not make View, Binder, UIListView, MessageBus, or generated code access business objects.
- Do not put gameplay model types in Source JSON. Map business models to Store fields or `MessagePayload`.
- Do not create DemoService implementations by default. If no business exists, keep the UI pure and mark Runtime pending.

## Adapter Shape

```csharp
public interface IPlayerHudService
{
    event Action StatsChanged;
    PlayerHudSnapshot GetSnapshot();
}

public sealed class PlayerHudServiceAdapter : MonoBehaviour, IPlayerHudService
{
    [SerializeField] private PlayerController _playerController;

    private UIManager _registeredManager;

    public event Action StatsChanged;

    private void Start()
    {
        _playerController.HealthChanged += HandleStatsChanged;
        _playerController.ManaChanged += HandleStatsChanged;

        _registeredManager = UIManager.Instance;
        if (_registeredManager == null)
        {
            Debug.LogError("PlayerHudServiceAdapter requires a UIManager in the scene.");
            return;
        }

        _registeredManager.RegisterService<IPlayerHudService>(this);
    }

    private void OnDestroy()
    {
        if (_playerController != null)
        {
            _playerController.HealthChanged -= HandleStatsChanged;
            _playerController.ManaChanged -= HandleStatsChanged;
        }

        if (_registeredManager != null)
        {
            _registeredManager.UnregisterService<IPlayerHudService>();
            _registeredManager = null;
        }
    }

    public PlayerHudSnapshot GetSnapshot()
    {
        return new PlayerHudSnapshot(
            _playerController.Health,
            _playerController.MaxHealth,
            _playerController.Mana,
            _playerController.MaxMana);
    }

    private void HandleStatsChanged()
    {
        StatsChanged?.Invoke();
    }
}
```

`PlayerHudSnapshot` can be a small UI-facing DTO owned by the adapter layer. It must not be a framework runtime type.
