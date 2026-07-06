# Changelog

## Unreleased

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
