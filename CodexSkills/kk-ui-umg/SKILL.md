---
name: kk-ui-umg
description: Create, modify, validate, or review KK_UI_UMG MVVM-C Unity UGUI Source packages and business service adapters. Use when Codex is asked to build UI from natural language, edit Assets/UI/Source manifests, plan bindings/events/assets/layoutComponents, connect existing business code through requiredServices adapters, review MVVM-C boundaries, or run Validate/Generate/Verify without touching Generated files.
---

# KK_UI_UMG Authoring

## Overview

Use this skill to work on the KK_UI_UMG MVVM-C Unity UI pipeline. Source packages live under `Assets/UI/Source/<PackageId>/`; generated C# and UGUI prefabs are rebuildable outputs and must not be hand-edited.

When a user has just imported the package and wants to create their own UI, this skill should be enough to author the Source package JSON files, explain the generated output, and explain the runtime `UIManager` setup needed to open the UI in a scene.

## Core Contract

- Read existing Source files before changing them.
- Do not guess existing node ids, field ids, handler names, asset ids, or loc keys.
- Do not edit `Assets/UI/Generated/`.
- Do not write business Controller implementation unless the user explicitly asks for handwritten business code.
- New UI authoring is UI-first by default. Do not add `requiredServices` until the user asks to connect business data.
- If a UI needs existing business data or commands, declare `codegen.requiredServices` and create a UI-facing service adapter; do not put business model types or query logic into Source JSON.
- Handwritten Controller partials live at `Assets/UI/<PackageId>/<PackageId>Controller.cs`. Do not create `Controllers/`, `Business/`, or `Partial/` subfolders, and do not place handwritten partials under `Assets/UI/Generated/`.
- If stale Generated scripts would make a generated service property unavailable, handwritten Controller code may call `RequireService<T>()` directly while keeping `codegen.requiredServices` declared.
- Do not create DemoService implementations by default. If no business exists, leave Runtime pending.
- Do not introduce TwoWay automatic binding.
- Do not attach arbitrary Unity components.
- Use only supported manifest controls and `layoutComponents`.
- Prefer current Source examples over copied generated output.
- Route input events into Controller handlers only.
- Keep View, Store, Binder, Controller, UIManager responsibilities separated.
- Runtime opening requires a scene GameObject with the `KK.UI.UMG.UIManager` component attached.
- Run Validate / Generate / Verify when Unity Editor access is available; otherwise tell the user exactly what remains to run.

## Workflow

### Create UI

1. Extract the UI name, purpose, regions, controls, display data, list data, events, static text, and assets from the user request.
2. Ask only for missing information that would change the schema or behavior. Use safe defaults for layout and naming.
3. Read `references/schema-v054.md` and the relevant examples listed in `references/examples.md`.
4. Create `Assets/UI/Source/<PackageId>/package.json`, `layout.json`, `bindings.json`, `codegen.json`, `strings.json`, `assets.json`, `README.md`, `validation.md`, and optional `Assets/`.
5. Use LayoutComponents first for structure, then `layoutElement`, then rect fine-tuning.
6. Validate, Generate, Verify if possible.
7. Ensure `README.md` describes the package and `validation.md` contains the v0.7.1 ledger markers.
8. Report the delivery status, any handwritten Controller handlers still needed, and the runtime setup note: add `KK.UI.UMG.UIManager` to a scene GameObject before calling `UIManager.Instance.OpenAsync("<PackageId>")`.

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
5. Put or update the handwritten Controller partial at `Assets/UI/<PackageId>/<PackageId>Controller.cs` so the UI only depends on the service interface.
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
4. Run `Validate`, `Generate`, and `Verify` from `KK_UI_UMG/KKPipeline`.
5. Open generated UI with `await UIManager.Instance.OpenAsync("<PackageId>");`.
6. Close generated UI with `await UIManager.Instance.CloseAsync("<PackageId>");`.

`UIManager` owns runtime UI lifecycle:

- Loads generated prefabs through Addressables key `UI/<PackageId>/<PackageId>View`.
- Instantiates the prefab under the UI root.
- Creates one Controller instance per open UI lifecycle through generated Controller factory registration.
- Calls Controller lifecycle in order: `BindView`, `OnPreOpen`, `Initialize`, `Flush`, `OnOpened`, `OnActivated`.
- Maintains open state, active views, active controllers, Addressables handles, and top-first layer stack.
- Releases Addressables handles and disposes the Controller on close.
- Provides `IsOpen`, `GetState`, `GetTopLayer`, and `GetLayerStack`.
- Provides service registration for business adapters: `RegisterService<T>`, `TryGetService<T>`, `UnregisterService<T>`, and `ClearServices`.
- Subscribes generated MessageBus routes so configured bus messages can open or close UI through `UIManager`.

Generated runtime code registers Controller factories during load. Do not put Controller components on the prefab and do not make Controller singletons. The prefab carries the generated View; `UIManager` creates the Controller for each Open and destroys it on Close.

When a UI declares `codegen.requiredServices`, the scene must register those services on `UIManager` before opening that UI. Business adapters should register in `OnEnable` and unregister in `OnDisable` or equivalent lifecycle code.

## References

- Read `references/schema-v054.md` when creating a package, touching `layoutComponents`, adding supported controls, binding lists, or handling assets.
- Read `references/authoring-checklist.md` before finalizing changes or explaining delivery status.
- Read `references/examples.md` before choosing a pattern for dialogs, inventory/list panels, or layout component galleries.
- Read `references/business-adapter.md` before connecting UI to existing gameplay/business code.

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

The pipeline now scaffolds and updates the marker block for Validate / Generate / Verify / Preview. Do not overwrite content outside the markers. Do not save preview screenshots or write screenshot paths. Runtime remains `Pending` unless a PlayMode or manual runtime check explicitly verified behavior.

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

Project-level `Assets/` content is consumer/example content and is not part of the pipeline package release contract. Do not use the state of `Assets/UI/Source/...`, `Assets/UI/Generated/...`, or scene bootstrap scripts as evidence that the UPM package is or is not releasable.

The UPM release tarball should include `Runtime/`, `Editor/`, `CodexSkills/`, `README.md`, `CHANGELOG.md`, `LICENSE.md`, and `package.json`. By default, release packaging excludes package development `Tests/`; tests remain in the source repository unless fixtures are moved into the package or samples are published through `Samples~`.
