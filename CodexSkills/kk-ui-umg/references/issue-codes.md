# KK_UI_UMG Issue Code Reference

Use this reference when explaining Validate / Generate / Verify failures.

| Code | Fix Direction |
|---|---|
| `SRC001` | Put the Source package under `Assets/` or `Packages/`, make the final folder name match `packageId`, and do not put it under a `Generated` folder. |
| `TXT001` | Move static Text copy to `strings.json` and reference it with `layout.json` `locKey`. |
| `TXT002` | Add the missing `locKey` to the `defaultCulture` table in `strings.json`. |
| `TXT003` | A Text node cannot use both `locKey` and dynamic `text` binding; choose one path. |
| `TXT004` | Static button labels should use child Text `locKey`, not Store fields. |
| `TXT005` | Remove unused string keys or reference them from `layout.json`. |
| `AST004` | Use an existing `Assets/...` or `Packages/...` file for `asset.source`. |
| `AST005` | Keep package-owned assets under `Source/Assets` or add a narrow `sharedAssetRoots` entry. |
| `AST006` | Copied package assets must resolve under `<Generated Parent>/<PackageId>/Assets`. |
| `AST008` | Missing `contentHash` is a warning in v1.0.x; copy the reported `sha256:` to make it strict. |
| `AST009` / `AST012` | Recompute and update the `sha256:` content hash. |
| `GEN003` | Use default `Assets/UI/Generated/<PackageId>` or select `Generated Parent Folder`. |
| `GEN006` | Use Addressables key `UI/<PackageId>/<PackageId>View`. |
| `GEN007` | Use `UIViewBase` for View and `UIControllerBase` for Controller. |
| `GEN008` / `GEN009` | Generated Parent must be under `Assets/` or `Packages/`, and output is `<Generated Parent>/<PackageId>`. |
| `CG020`-`CG023` | Fix `requiredServices` type/property entries; properties must be valid unique C# identifiers. |
| `GENPENDING` | Wait for Unity compilation; prefab generation should continue automatically. |
