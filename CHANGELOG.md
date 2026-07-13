# Changelog

## Unreleased

## 1.0.5

- Added optional handwritten View partial hooks for Open, Show, Hide, and Close transitions; `UIManager` now awaits them with per-panel serialization, cross-panel concurrency, interaction gating, cancellation, timeout, and rollback.
- Made `LayerStack` topology-only and added arbitrary layer removal so a transition can safely finish after another panel has covered it.
- Generated stable `RectTransform` references for named nested Panel nodes without duplicating existing control references or the View root.
- Added legacy `Runtime: Pass` ledger migration to canonical `Verified`, plus explicit `Mark Runtime Verified` and `Reset Runtime Pending` actions in KKPipeline.
- Added runtime, generator, verifier, and validation-ledger regression coverage for the v1.0.5 contracts.

## 1.0.4

- Added runtime preload and panel cache APIs: `PreloadAsync`, `HideAsync`, `ShowAsync`, `ReleaseAsync`, and `CloseAsync(systemId, UICloseMode.Hide)`.
- Refactored `UIManager` runtime responsibilities into loader, service registry, bus router, layer manager, and panel cache collaborators while preserving existing `OpenAsync` / `CloseAsync` usage.
- Added low-GC runtime cleanup: layer stack snapshot caching, shared UGUI apply helper, `UIListView` item pooling, and `ViewModelStore.TakeDirty` buffer reuse while keeping generic `Store.Update<T>` as the Store update API.

## 1.0.3

- Allowed Source packages to use custom roots under `Assets/` or `Packages/`; the final Source package folder must match `packageId` and must not be under a `Generated` folder.
- Updated `KKPipeline` manifest auto-discovery and documentation for custom Source roots such as `Assets/_Project/UISource/<PackageId>/`.

## 1.0.2

- Added a first-run checklist in `KK_UI_UMG/Setting` for Codex Skill, scene UIManager, and Addressables settings.
- Added `Create UIManager In Scene` to the Setting window.
- Improved `KKPipeline` result summaries with output path, prefab path, Addressables key, runtime `OpenAsync` snippet, and next-step guidance.
- Added issue-code fix hints in the Editor result panel and documented common issue codes in `ISSUE_CODES.md`.
- Clarified README, sample README, and Skill guidance that the package sample Source is the primary AI authoring template; Generated output remains rebuildable output.
- Clarified install boundary: release tarball is recommended for ordinary users, while Git URL is better for following source/development.
- Removed extra Sample and Diagnostics top-level menu shortcuts; the sample remains visible under the package `Sample/` folder.
- Removed the user-facing `Build Package` menu item; package builds remain available through command line or internal build calls.

## 1.0.1

- Added package-contained `Inventory Panel Sample` under `Sample/InventoryPanelSample`.
- Added `KK_UI_UMG/Sample/Open Inventory Panel Sample` to register the package prefab Addressables key and open the package sample scene.
- Added Editor-selectable `Generated Parent Folder` so Generate / Verify / Preview write each UI under `<Generated Parent>/<PackageId>`.
- Updated business Controller partial convention to `<Generated Parent>/<PackageId>/<PackageId>Controller.cs`; generated-owned files remain under `Scripts/`, `Prefabs/`, `Reports/`, and `Assets/`.
- Updated package and skill documentation to describe the sample workflow.
- Fixed package sample Source asset validation by allowing existing `Packages/` asset paths while preserving Source/Assets and shared asset root boundaries.
- Clarified README wording around Unity Newtonsoft Json as an internal Editor pipeline dependency.
- Updated Skill service adapter examples to register in `Start()` and unregister in `OnDestroy()`.

## 1.0.0

- Added v0.9.3 Text / Localization authoring rules: static UI copy uses `locKey` plus `strings.json`, while runtime-changing text uses bindings, Store, and Binder.
- Validator now reports `TXT001`, `TXT002`, `TXT003`, `TXT004`, and `TXT005` for static/dynamic Text boundary issues.
- Generated Controllers no longer initialize Store fields from static `locKey` text.
- Package id is `com.kk.ui-umg`.
- Display name is `KK_UI_UMG`.
- Runtime namespace is `KK.UI.UMG`.
- Added package README, CHANGELOG, and LICENSE.
- Hardened package release scope around `Packages/com.kk.ui-umg`.
- Release tarball excludes package development `Tests/` by default.
- Project-level `Assets/` examples are not part of the package release contract.
