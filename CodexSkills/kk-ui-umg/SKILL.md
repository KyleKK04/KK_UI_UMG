---
name: kk-ui-umg
description: Create, modify, validate, or review KK_UI_UMG MVVM-C Unity UGUI Source packages and business service adapters. Use when Codex is asked to build UI from natural language, edit Source package manifests, plan bindings/events/assets/layoutComponents, connect existing business code through requiredServices adapters, review MVVM-C boundaries, or run Validate/Generate/Verify without touching Generated files.
---

# KK_UI_UMG Authoring

## Overview

Use this skill to work on the KK_UI_UMG MVVM-C Unity UI pipeline. Source packages default to `Assets/UI/Source/<PackageId>/`, but projects may choose another root under `Assets/` or `Packages/` as long as the final folder name matches `packageId`. Generated C# and UGUI prefabs are rebuildable outputs. A designer may adjust supported layout values in a generated prefab only when those changes are explicitly captured back into Source with KKPipeline `Export` before the next generation.

When a user has just imported the package and wants to create their own UI, this skill should be enough to author the Source package JSON files, explain the generated output, and explain the runtime `UIManager` setup needed to open the UI in a scene.

## Core Contract

- Read existing Source files before changing them.
- Do not guess existing node ids, field ids, handler names, asset ids, or loc keys.
- Do not edit generated-owned files under `<Generated Parent>/<PackageId>/Scripts`, `Reports`, or `Assets`. Generated Prefab layout edits are allowed only as temporary authoring input: save the Prefab, select it in the Project window, and run KKPipeline `Export`; unsupported Prefab changes remain disposable.
- Do not write business Controller implementation unless the user explicitly asks for handwritten business code.
- New UI authoring is UI-first by default. Do not add `requiredServices` until the user asks to connect business data.
- If a UI needs existing business data or commands, declare `codegen.requiredServices` and create a UI-facing service adapter; do not put business model types or query logic into Source JSON.
- Handwritten business Controller partials live at `<Generated Parent>/<PackageId>/<PackageId>Controller.cs`, next to `Scripts/`, `Prefabs/`, `Reports/`, and `Assets/`. Do not create `Controllers/`, `Business/`, or `Partial/` subfolders, and do not place handwritten partials inside the generated-owned `Scripts/` folder.
- Optional handwritten visual transition partials live at `<Generated Parent>/<PackageId>/<ViewClassName>.cs`. Write them only when the user requests animation; never put them in generated-owned `Scripts/` and never edit `<ViewClassName>.Generated.cs`.
- Lifecycle animation belongs in the View partial through Open / Show / Hide / Close transition overrides. Do not put animation in Controller partials, Binder, Store, or Source JSON.
- Pass the framework `CancellationToken` through every awaited animation. View transition code may change visual properties only and must not call UIManager or Controller lifecycle methods.
- If stale Generated scripts would make a generated service property unavailable, handwritten Controller code may call `RequireService<T>()` directly while keeping `codegen.requiredServices` declared.
- Static UI copy uses `layout.json` `locKey` plus `strings.json`. Static titles, labels, button text, placeholders, section headers, and fixed empty prompts do not need `bindings.json` fields, Store fields, Controller initialization, or Binder refresh.
- Dynamic Text enters `bindings.json` only when it changes at runtime, comes from business data, service callbacks, counts, progress, status, player data, item data, task data, or list item data.
- Do not put runtime data such as player names, item names, counts, task instance text, or service error details in `strings.json`.
- A Text node must not have both a static `locKey` and a dynamic `text` binding.
- Do not create DemoService implementations by default. If no business exists, leave Runtime pending.
- Do not introduce TwoWay automatic binding.
- Do not attach arbitrary Unity components.
- Use only supported manifest controls and `layoutComponents`.
- Use built-in dynamic binding properties for common UI state: Text color/alpha/fontSize, Image color/alpha/fillAmount/raycastTarget, RawImage color/alpha, Button visual color/alpha, Slider min/max/value, Scrollbar value/size, and standard interactable/value/text bindings. Do not bypass Store/Binder from Controller for these common properties.
- Source package roots must stay under `Assets/` or `Packages/`, the final folder name must match `packageId`, and Source must not be placed under a `Generated` folder.
- Asset `contentHash` is an optional content lock. Font assets should prioritize a valid `TMP_FontAsset` asset reference through `assets.json` `source`; do not add `contentHash` just to satisfy validation. Add `sha256:` only when the user explicitly wants to lock a stable font asset version. Also omit `contentHash` for other Unity-rewritten assets such as dynamic atlases and materials.
- For new UI packages, use `Sample/InventoryPanelSample/Source/KkSampleInventoryPanel` as the primary package-contained template.
- Prefer current Source examples over copied generated output.
- Never use sample or project Generated output as the authoring template for new Source JSON.
- Route input events into Controller handlers only.
- Keep View, Store, Binder, Controller, UIManager responsibilities separated.
- Runtime opening requires a scene GameObject with the `KK.UI.UMG.UIManager` component attached.
- After creating or modifying a UI, explain the `UIManager` interface needed to open the generated prefab at runtime.
- For frequently toggled UI such as inventory, pause, map, settings, or HUD panels, recommend `PreloadAsync` plus `HideAsync` / `ShowAsync` instead of repeatedly destroying and reopening the panel.
- Store writes continue to use generic `Store.Update<T>`; do not invent `UpdateInt` / `UpdateFloat` / `UpdateBool` APIs.
- Run Validate / Generate / Verify when Unity Editor access is available; otherwise tell the user exactly what remains to run.
- Runtime ledger output uses only `Pending` or `Verified`. Legacy `Runtime: Pass` is read as `Verified`; never author new Runtime `Pass` rows.
- Mark Runtime `Verified` only after a real PlayMode check, using the KKPipeline `Runtime Verify` confirmation button. Generate automatically resets Runtime to `Pending`; static pipeline success is not runtime verification.

## Workflow

### Create UI

1. Extract the UI name, purpose, regions, controls, display data, list data, events, static text, dynamic text, and assets from the user request.
2. Ask only for missing information that would change the schema or behavior. Use safe defaults for layout and naming.
3. Read `references/schema-v054.md` and the package sample Source listed in `references/examples.md`.
4. Create the Source package at the user's chosen Source root, defaulting to `Assets/UI/Source/<PackageId>/`, with `package.json`, `layout.json`, `bindings.json`, `codegen.json`, `strings.json`, `assets.json`, `README.md`, `validation.md`, and optional `Assets/`.
5. Use LayoutComponents first for structure, then `layoutElement`, then rect fine-tuning.
6. Validate, Generate, Verify if possible.
7. Ensure `README.md` describes the package and `validation.md` contains the v0.7.1 ledger markers.
8. Report the delivery status, any handwritten Controller handlers still needed, and the runtime setup note: add `KK.UI.UMG.UIManager` to a scene GameObject before calling `UIManager.Instance.OpenAsync("<PackageId>")`.
9. Include the minimal `UIManager` call pattern for the generated prefab: `await UIManager.Instance.OpenAsync("<PackageId>");`.
10. When reporting Store fields, list dynamic Store fields only. Static locKey copy is not a Store field.
11. If the user requested lifecycle animation, add the handwritten View partial beside `Scripts/` only after generated component references are known, then run the runtime transition checks.

### Modify UI

1. Read every Source manifest in the package before editing.
2. If changing lists, also inspect the `VerticalList` item template, item bindings, and item events.
3. Update affected files together: layout, bindings, strings, assets, codegen.
4. Preserve generated/handwritten boundaries.
5. Validate, Generate, Verify if possible.

### Connect Business Service

1. Read the UI Source package and handwritten Controller partial.
2. Read the existing business type requested by the user, such as `PlayerController`.
3. Read `references/business-adapter.md`.
4. Generate or update a UI-facing `I<Feature>Service` contract and one adapter MonoBehaviour in the business directory.
5. Put or update the handwritten Controller partial at `<Generated Parent>/<PackageId>/<PackageId>Controller.cs` so the UI only depends on the service interface.
6. Update `codegen.requiredServices`.
7. If the business type lacks public getters or changed events, add the minimum public API/event needed; never use reflection to read private fields.
8. Validate, Generate, Verify if possible, then report Runtime pending or verified.

### Review MVVM-C Boundary

Check that View, Binder, UIListView, MessageBus, and generated code do not access business services or models. Business access must flow through Controller partial and UI-facing service adapters.

## Runtime Setup And UIManager

For a newly imported package, explain this runtime setup whenever the user asks how to run or open generated UI:

1. Add a GameObject in the scene, commonly named `UIManager`.
2. Attach the package runtime component `KK.UI.UMG.UIManager` to that GameObject.
3. A child `Canvas` is optional. `UIManager` will use an existing child/scene `Canvas` or create a `UIRoot` Canvas and `EventSystem` if needed.
4. In `Generated Parent Folder`, choose where generated UI folders are written. The default is `Assets/UI/Generated`, and each package writes to `<Generated Parent>/<PackageId>/`.
5. Run `Validate`, `Generate`, and `Verify` from `KK_UI_UMG/KKPipeline`.
6. Open generated UI with `await UIManager.Instance.OpenAsync("<PackageId>");`.
7. Close generated UI with `await UIManager.Instance.CloseAsync("<PackageId>");`.
8. For high-frequency panels, preload once and toggle visibility with `HideAsync` / `ShowAsync`.

`UIManager` owns runtime UI lifecycle:

- Loads generated prefabs through Addressables key `UI/<PackageId>/<PackageId>View`.
- Instantiates the prefab under the UI root.
- Creates one Controller instance per open UI lifecycle through generated Controller factory registration.
- Calls Controller lifecycle in order around the View transition: `BindView`, `OnPreOpen`, `Initialize`, `Flush`, `OnOpened`, await Open transition, then `OnActivated` only if the View is the ready top layer.
- Maintains open / hidden state, active and hidden panel instances, Addressables handles, and top-first layer stack.
- Preloads generated prefab assets through `PreloadAsync`.
- Hides high-frequency panels through `HideAsync` without disposing Controller, destroying View, clearing Store, or releasing Addressables.
- Shows hidden panels through `ShowAsync` without re-instantiating, rebinding, or reinitializing.
- Awaits optional View Open / Show / Hide / Close transitions, serializes opposite commands for the same View, and allows different View instances to transition concurrently.
- Keeps transition targets non-interactable, prevents click-through, and activates only a transition-complete top layer.
- Uses a five-second real-time transition timeout by default. Ordinary transition exceptions are logged and degraded to the target state; timeouts cancel and roll back before throwing `TimeoutException`.
- Releases Addressables handles and disposes the Controller on `CloseAsync` / `ReleaseAsync`.
- Provides `IsOpen`, `GetState`, `GetTopLayer`, and `GetLayerStack`.
- Provides service registration for business adapters: `RegisterService<T>`, `TryGetService<T>`, `UnregisterService<T>`, and `ClearServices`.
- Subscribes generated MessageBus routes so configured bus messages can open or close UI through `UIManager`.

Use this public interface cheat sheet when explaining how a generated prefab is used:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using KK.UI.UMG;
using KK.UI.UMG.Internal;
using UnityEngine;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; }

    public Task PreloadAsync(string systemId);
    public Task<UIViewBase> OpenAsync(string systemId, MessagePayload payload = null);
    public Task<UIViewBase> ShowAsync(string systemId, MessagePayload payload = null);
    public Task HideAsync(string systemId);
    public Task CloseAsync(string systemId);
    public Task CloseAsync(string systemId, UICloseMode mode);
    public Task ReleaseAsync(string systemId);

    public bool IsOpen(string systemId);
    public UIState GetState(string systemId);
    public UIViewBase GetTopLayer();
    public IReadOnlyList<string> GetLayerStack();

    public void RegisterService<T>(T service) where T : class;
    public bool TryGetService<T>(out T service) where T : class;
    public void UnregisterService<T>() where T : class;
    public void ClearServices();
}
```

Minimal open example:

```csharp
using KK.UI.UMG;
using UnityEngine;

public sealed class UIBootstrap : MonoBehaviour
{
    private async void Start()
    {
        await UIManager.Instance.OpenAsync("InventoryPanel");
    }
}
```

Payload example:

```csharp
var payload = new MessagePayload();
payload.Set("itemId", "sword_001");
await UIManager.Instance.OpenAsync("ItemDetailPanel", payload);
```

High-frequency panel example:

```csharp
await UIManager.Instance.PreloadAsync("InventoryPanel");
await UIManager.Instance.OpenAsync("InventoryPanel");
await UIManager.Instance.HideAsync("InventoryPanel");
await UIManager.Instance.ShowAsync("InventoryPanel");
await UIManager.Instance.ReleaseAsync("InventoryPanel");
```

Generated prefabs are not normally opened by dragging them into the scene. The runtime path is `UIManager.OpenAsync(systemId)`, where `systemId` is the Source `packageId` and the Addressables key is `UI/<PackageId>/<PackageId>View`.

Generated runtime code registers Controller factories during load. Do not put Controller components on the prefab and do not make Controller singletons. The prefab carries the generated View; `UIManager` creates the Controller for each first Open and destroys it on Close / Release. Hide / Show keeps the existing View, Controller, and Store alive.

When a UI declares `codegen.requiredServices`, the scene must register those services on `UIManager` before opening that UI. Business adapters should register in `Start` and unregister in `OnDestroy` or an equivalent lifecycle that does not depend on `Awake` / `OnEnable` ordering across GameObjects.

## Package Sample

The package ships one visible sample inside the package:

```text
Packages/com.kk.ui-umg/Sample/InventoryPanelSample/
```

Use this sample as the reference for a complete business-backed UI:

- Source JSON at `Sample/InventoryPanelSample/Source/KkSampleInventoryPanel`.
- Generated quick-start output at `Sample/InventoryPanelSample/Generated/KkSampleInventoryPanel`.
- Handwritten Controller partial at `Sample/InventoryPanelSample/Generated/KkSampleInventoryPanel/KkSampleInventoryPanelController.cs`.
- Demo business service under `Sample/InventoryPanelSample/Scripts/Inventory`.
- Runtime open path through `UIManager.Instance.OpenAsync("KkSampleInventoryPanel")`.

The included Generated files are package sample artifacts. For new project work, keep project Source JSON under a project-owned Source package root such as `Assets/UI/Source/<PackageId>` or `Assets/_Project/UISource/<PackageId>` as the source of truth and do not hand-edit project Generated files.
When creating a new UI, read the sample Source JSON first and adapt its Source-side patterns. Do not copy from the sample Generated scripts or prefab.

## References

- Read `references/schema-v054.md` when creating a package, touching `layoutComponents`, adding supported controls, binding lists, or handling assets.
- Read `references/authoring-checklist.md` before finalizing changes or explaining delivery status.
- Read `references/examples.md` before choosing a pattern for dialogs, inventory/list panels, or layout component galleries.
- Read `references/business-adapter.md` before connecting UI to existing gameplay/business code.
- Read `references/uimanager-runtime.md` before explaining runtime open/close code, writing bootstrap examples, or connecting a generated prefab to scene code.
- Read `references/issue-codes.md` before explaining Validate / Generate / Verify failures.

## MVVM-C Rules

Keep the runtime chain unique:

```text
UGUI event
  -> View.Generated handler
  -> Controller handler
  -> Store.Update
  -> Flush
  -> Binder writes UGUI
```

View forwards events only. Controller owns business and state transitions. Store is written only by Controller. Binder writes UGUI only. UIManager owns Open / Close / lifecycle. Runtime must not read Source JSON.

Static Text is generated through `layout.json` `locKey` and `strings.json` during prefab generation. Controller, View, and Binder do not resolve static localization at runtime. Dynamic Text is the only text path that should use Controller `Store.Update`, `Flush`, and Binder writes.

When external business data is required, the runtime chain is:

```text
Business Service / Model
  -> Controller partial
  -> Store.Update
  -> Flush
  -> Binder writes UGUI
```

View, Binder, UIListView, and MessageBus must not access business services. Controller may cache UI state and business ids, but not long-lived copies of business source lists.

## Delivery Status

Every Source package must include:

```text
README.md
validation.md
```

`README.md` is human package documentation. `validation.md` is the delivery ledger. It must contain the pipeline-owned marker block:

```md
<!-- ui-pipeline:validation-ledger:start -->
...
<!-- ui-pipeline:validation-ledger:end -->
```

The pipeline now scaffolds and updates the marker block for Validate / Generate / Verify / Preview. Do not overwrite content outside the markers. Runtime remains `Pending` unless a PlayMode check explicitly verified behavior and the user confirms it with `Runtime Verify`. Generate automatically resets Runtime to `Pending`. Legacy `Runtime: Pass` is migrated to `Verified` on the next ledger write.

Report one status:

- `Source authoring complete`: Source JSON is created or modified and internally consistent.
- `Generated prefab verified`: Validate / Generate / Verify passed.
- `Runtime behavior pending`: handwritten Controller business behavior is still needed.
- `Runtime behavior verified`: handwritten Controller behavior has passed PlayMode validation.

Only Source JSON work can reach `Generated prefab verified`. Do not claim runtime behavior is verified unless the Controller implementation and PlayMode path were actually tested.

## Package Installation Source

The project keeps this skill source under:

```text
Packages/com.kk.ui-umg/CodexSkills/kk-ui-umg/
```

Install it into Codex by running:

```bash
python3 Packages/com.kk.ui-umg/CodexSkills/kk-ui-umg/scripts/install_skill.py
```

Then validate the installed folder with:

```bash
python3 ~/.codex/skills/kk-ui-umg/scripts/quick_validate.py
```

## Package Release Scope

When hardening or packaging KK_UI_UMG itself, the release object is:

```text
Packages/com.kk.ui-umg/
```

Project-level `Assets/` content is consumer/example content and is not part of the pipeline package release contract. Do not use the state of project Source packages, project Generated output, or scene bootstrap scripts as evidence that the UPM package is or is not releasable.

The UPM release tarball should include `Runtime/`, `Editor/`, `CodexSkills/`, `Sample/`, `README.md`, `CHANGELOG.md`, `LICENSE.md`, and `package.json`. By default, release packaging excludes package development `Tests/`; tests remain in the source repository.
