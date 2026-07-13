# Authoring Checklist

Use this checklist before returning work to the user.

## Before Editing

- Read the relevant Source package files.
- For new UI packages, read the closest Source examples listed in `examples.md`.
- Identify whether the user asked for Source authoring only, runtime behavior, or both.
- Treat new UI requests as UI-first unless the user explicitly asks to connect business data.
- Classify text before writing manifests: static copy uses `locKey` + `strings.json`; runtime-changing text uses `bindings.json` + Store.
- Confirm missing information only when guessing would change schema, ids, assets, or behavior.

## New Package

- Create Source package files under the user's chosen Source root. Default to `Assets/UI/Source/<PackageId>/` when the project has no stronger convention.
- Keep the Source package root under `Assets/` or `Packages/`, make the final folder name match `packageId`, and never place Source under a `Generated` folder.
- Write `package.json`, `layout.json`, `bindings.json`, `codegen.json`, `strings.json`, `assets.json`, `README.md`, and `validation.md`.
- Keep node ids, field ids, loc keys, and asset ids stable and unique.
- Put static titles, button text, labels, placeholders, section headers, and fixed empty prompts in `strings.json` and reference them by `layout.json` `locKey`.
- Do not create Store fields or `text` bindings for static copy.
- Create Text fields and `text` bindings only for runtime-changing, business-derived, count/progress/status, player/item/task, or list item text.
- Use `layoutComponents` for structural layout.
- Use `layoutElement` for stable button, input, text, and list item sizing.
- Keep `v1.controls` limited to the controls used.
- Use `addressablesKey: UI/<PackageId>/<PackageId>View`.
- In `KK_UI_UMG/KKPipeline`, use `Generated Parent Folder` to choose the output parent. The default is `Assets/UI/Generated`; generated output goes to `<Generated Parent>/<PackageId>/`.
- In `README.md`, record UI name, packageId, purpose, entry path, main areas, controls, layoutComponents, generated output, and delivery status.
- In `validation.md`, include the v0.7.1 pipeline ledger markers. The pipeline owns only the marker block; keep manual notes outside it.
- For runtime setup notes, tell the user to add a scene GameObject with `KK.UI.UMG.UIManager`, then open the UI with `UIManager.Instance.OpenAsync("<PackageId>")`.

## Existing Package

- Read every manifest before changing one.
- Do not guess ids or handlers.
- For a new Button, update layout, bindings events, and strings.
- For static Button labels, update the child Text `locKey` and `strings.json`; do not add a ButtonText Store field or binding.
- For a new Toggle / Slider / InputField / Dropdown, update layout, bindings fields, property bindings, events, and strings.
- For a new list field, update the `VerticalList` item template and item bindings.
- For a new image or sprite, verify the real asset path and update `assets.json`.
- If the user asks to connect external business data, read `business-adapter.md`, add `codegen.requiredServices`, and use a UI-facing service adapter instead of inventing business model fields in `bindings.json`.
- Handwritten Controller partials belong at `<Generated Parent>/<PackageId>/<PackageId>Controller.cs`; do not create `Controllers/`, `Business/`, or `Partial/` subfolders and do not put them inside the generated-owned `Scripts/` folder.
- Handwritten View transition partials belong at `<Generated Parent>/<PackageId>/<ViewClassName>.cs`; create one only when animation is requested, and never place it in `Scripts/`.
- Keep animation in View Open / Show / Hide / Close overrides, forward the supplied `CancellationToken`, and do not call UIManager or Controller lifecycle from animation code.
- Report any new or changed handwritten Controller handlers required.

## MVVM-C Boundary

- View forwards events only.
- Controller handles behavior and writes Store.
- Store is the only display state source.
- Binder reads Store and writes UGUI only.
- Static locKey text is written during prefab generation. Controller, View, and Binder do not resolve static localization at runtime.
- A Text node cannot have both static `locKey` and dynamic `text` binding.
- UIListView does not call business logic.
- MessageBus does not create View or Controller.
- Business services are accessed only from handwritten Controller partial code.
- Existing gameplay classes are wrapped by business-directory adapters; UI code must not depend on concrete gameplay classes.
- If stale Generated scripts would make a generated service property unavailable, resolve the service with `RequireService<T>()` directly while keeping `codegen.requiredServices` declared.
- View, Binder, UIListView, and MessageBus do not access business services.
- Controller stores UI state and business ids, not long-lived copies of business source lists.
- Runtime code does not read Source JSON.
- Runtime ledger statuses are `Pending / Verified`; do not author `Runtime: Pass`. Mark Verified only after a real runtime check with concrete notes.
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
- Use `references/issue-codes.md` to explain issue codes with concise fix directions.

## Final Report

Include:

- Source package path changed or created.
- Manifest files changed.
- Dynamic Store fields generated or changed. Do not list static locKey copy as Store fields.
- Controller events generated or changed.
- Handwritten Controller handlers still needed.
- Required business services declared or changed.
- Runtime setup reminder: scene needs a GameObject with `KK.UI.UMG.UIManager`, then open with `UIManager.Instance.OpenAsync("<PackageId>")`.
- Validate / Generate / Verify result or why it was not run.
- Preview result if run; otherwise state it remains `NotRun`.
- `README.md` and `validation.md` ledger status.
- Whether Runtime remains `Pending` or was explicitly marked `Verified` after PlayMode/manual evidence.
- Delivery status: `Source authoring complete`, `Generated prefab verified`, `Runtime behavior pending`, or `Runtime behavior verified`.
