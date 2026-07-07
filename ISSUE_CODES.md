# KK_UI_UMG Issue Codes

This page explains common Validate / Generate / Verify issues and the usual fix direction.

## Text And Localization

| Code | Meaning | Fix |
|---|---|---|
| `TXT001` | Static Text uses literal copy. | Move the copy to `strings.json` and reference it with `layout.json` `locKey`. |
| `TXT002` | A `locKey` is missing from `strings.json`. | Add the key to the `defaultCulture` table. |
| `TXT003` | A Text node has both static `locKey` and dynamic text binding. | Choose one path: static `locKey` or runtime Store binding. |
| `TXT004` | Static button label is bound through Store. | Use a child Text `locKey` for labels that do not change at runtime. |
| `TXT005` | `strings.json` contains an unused key. | Remove it or reference it from `layout.json`. |

## Assets

| Code | Meaning | Fix |
|---|---|---|
| `AST004` | `asset.source` is missing or not an existing Unity asset path. | Use an existing `Assets/...` or `Packages/...` file. |
| `AST005` | Asset is outside `Source/Assets` and outside `sharedAssetRoots`. | Move package-owned assets into `Source/Assets` or add a narrow shared root. |
| `AST006` | Asset target is outside Generated assets. | Keep copied package assets under `<Generated Parent>/<PackageId>/Assets`. |
| `AST008` | `contentHash` is missing. | Optional in v1.0.x; copy the reported `sha256:` value for stricter verification. |
| `AST009` / `AST012` | Asset hash format or value is wrong. | Recompute and update the `sha256:` content hash. |

## Codegen And Runtime Contract

| Code | Meaning | Fix |
|---|---|---|
| `GEN003` | `outputRoot` does not resolve to the default Generated path. | Use `Assets/UI/Generated/<PackageId>` or select `Generated Parent Folder`. |
| `GEN006` | Addressables key does not match runtime convention. | Use `UI/<PackageId>/<PackageId>View`. |
| `GEN007` | View or Controller base class is unsupported. | Use `UIViewBase` and `UIControllerBase`. |
| `GEN008` / `GEN009` | Generated Parent override is invalid. | Choose an `Assets/` or `Packages/` folder; output must be `<Generated Parent>/<PackageId>`. |
| `CG020`-`CG023` | `requiredServices` entry is invalid. | Use a UI-facing service type and a valid unique C# property name. |
| `GENPENDING` | Generated scripts are compiling. | Wait for Unity compilation; prefab generation continues automatically. |
