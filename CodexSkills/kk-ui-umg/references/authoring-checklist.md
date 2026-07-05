# Authoring Checklist

Use this checklist before returning work to the user.

## Before Editing

- Read the relevant Source package files.
- For new UI packages, read the closest Source examples listed in `examples.md`.
- Identify whether the user asked for Source authoring only, runtime behavior, or both.
- Treat new UI requests as UI-first unless the user explicitly asks to connect business data.
- Confirm missing information only when guessing would change schema, ids, assets, or behavior.

## New Package

- Create only `Assets/UI/Source/<PackageId>/` files and optional `Assets/`.
- Write `package.json`, `layout.json`, `bindings.json`, `codegen.json`, `strings.json`, `assets.json`, `README.md`, and `validation.md`.
- Keep node ids, field ids, loc keys, and asset ids stable and unique.
- Use `layoutComponents` for structural layout.
- Use `layoutElement` for stable button, input, text, and list item sizing.
- Keep `v1.controls` limited to the controls used.
- Use `addressablesKey: UI/<PackageId>/<PackageId>View`.
- In `README.md`, record UI name, packageId, purpose, entry path, main areas, controls, layoutComponents, generated output, and delivery status.
- In `validation.md`, include the v0.7.1 pipeline ledger markers. The pipeline owns only the marker block; keep manual notes outside it.
- For runtime setup notes, tell the user to add a scene GameObject with `KK.UI.UMG.UIManager`, then open the UI with `UIManager.Instance.OpenAsync("<PackageId>")`.

## Existing Package

- Read every manifest before changing one.
- Do not guess ids or handlers.
- For a new Button, update layout, bindings events, and strings.
- For a new Toggle / Slider / InputField / Dropdown, update layout, bindings fields, property bindings, events, and strings.
- For a new list field, update the `VerticalList` item template and item bindings.
- For a new image or sprite, verify the real asset path and update `assets.json`.
- If the user asks to connect external business data, read `business-adapter.md`, add `codegen.requiredServices`, and use a UI-facing service adapter instead of inventing business model fields in `bindings.json`.
- Handwritten Controller partials belong at `Assets/UI/<PackageId>/<PackageId>Controller.cs`; do not create `Controllers/`, `Business/`, or `Partial/` subfolders and do not put them under `Assets/UI/Generated/`.
- Report any new or changed handwritten Controller handlers required.

## MVVM-C Boundary

- View forwards events only.
- Controller handles behavior and writes Store.
- Store is the only display state source.
- Binder reads Store and writes UGUI only.
- UIListView does not call business logic.
- MessageBus does not create View or Controller.
- Business services are accessed only from handwritten Controller partial code.
- Existing gameplay classes are wrapped by business-directory adapters; UI code must not depend on concrete gameplay classes.
- If stale Generated scripts would make a generated service property unavailable, resolve the service with `RequireService<T>()` directly while keeping `codegen.requiredServices` declared.
- View, Binder, UIListView, and MessageBus do not access business services.
- Controller stores UI state and business ids, not long-lived copies of business source lists.
- Runtime code does not read Source JSON.
- Prefab Inspector persistent events should be empty after generation.

## Validation

If Unity Editor access is available:

```text
Validate
Generate
Verify
Refresh Preview when preview evidence is requested
```

If Unity Editor access is unavailable:

- Stop at Source authoring.
- Tell the user to run Validate / Generate / Verify in the KK_UI_UMG window.
- Leave the v0.7.1 ledger at `NotRun` for Validate / Generate / Verify / Preview.
- Do not claim prefab or runtime verification.
- Do not save preview screenshots. Preview status is recorded as ledger state only.

Failure handling:

- Validate failure: fix Source JSON.
- Generate failure: inspect manifest and generator errors; do not edit Generated.
- Verify failure: check generated file count, prefab references, Addressables key, Inspector events, Source/Generated asset boundaries, and Canvas/root requirements.

## Final Report

Include:

- Source package path changed or created.
- Manifest files changed.
- Store fields generated or changed.
- Controller events generated or changed.
- Handwritten Controller handlers still needed.
- Required business services declared or changed.
- Runtime setup reminder: scene needs a GameObject with `KK.UI.UMG.UIManager`, then open with `UIManager.Instance.OpenAsync("<PackageId>")`.
- Validate / Generate / Verify result or why it was not run.
- Preview result if run; otherwise state it remains `NotRun`.
- `README.md` and `validation.md` ledger status.
- Delivery status: `Source authoring complete`, `Generated prefab verified`, `Runtime behavior pending`, or `Runtime behavior verified`.
