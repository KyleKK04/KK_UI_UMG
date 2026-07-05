# KK_UI_UMG Schema Reference

Use this reference when creating or modifying `Assets/UI/Source/<PackageId>/`.

## Source Package Files

Create or maintain this structure:

```text
Assets/UI/Source/<PackageId>/
├─ package.json
├─ layout.json
├─ bindings.json
├─ codegen.json
├─ strings.json
├─ assets.json
├─ README.md
├─ validation.md
└─ Assets/
```

Never create or edit `Assets/UI/Generated/<PackageId>/` directly.

`README.md` and `validation.md` are Source package documentation, not generated output.

`README.md` is required package documentation. It should record the UI purpose, packageId, entry path, main regions, generated output path, event contract, and current delivery status.

`validation.md` is the v0.7.1 delivery ledger. It must contain the pipeline-owned marker block:

```md
<!-- ui-pipeline:validation-ledger:start -->
...
<!-- ui-pipeline:validation-ledger:end -->
```

The pipeline may update only the marker block for Validate / Generate / Verify / Preview status. Preserve manual notes outside the markers. Do not save preview screenshots or write screenshot paths. Runtime remains `Pending` unless a PlayMode test or manual runtime check explicitly verified behavior.

## package.json

Required concepts:

```text
schemaVersion
packageId
namespace
version
designResolution
manifests
addressablesGroup
sharedAssetRoots
v1.controls
```

Rules:

- `manifests` uses the fixed manifest filenames.
- `v1.controls` lists only control types actually used by the UI.
- `sharedAssetRoots` must be narrow. Do not whitelist `Assets/`.
- Use project-verified TMP shared roots unless the project layout differs:

```json
[
  "Assets/TextMesh Pro/Resources/Fonts & Materials/",
  "Assets/TextMesh Pro/Shaders/",
  "Assets/TextMesh Pro/Fonts/"
]
```

## Supported Controls

Supported manifest `type` values:

```text
Panel
Text
Image
Button
RawImage
Toggle
Slider
InputField
Dropdown
Scrollbar
ScrollView
VerticalList
```

## layout.json

Prefer:

```text
structure regions > layoutComponents > layoutElement > rect fine tuning
```

Supported `layoutComponents`:

```text
layoutElement
horizontalLayout
verticalLayout
gridLayout
contentSizeFitter
aspectRatioFitter
```

Legal `childAlignment`:

```text
UpperLeft
UpperCenter
UpperRight
MiddleLeft
MiddleCenter
MiddleRight
LowerLeft
LowerCenter
LowerRight
```

Legal `gridLayout.startCorner`:

```text
UpperLeft
UpperRight
LowerLeft
LowerRight
```

Legal `gridLayout.startAxis`:

```text
Horizontal
Vertical
```

Legal `gridLayout.constraint`:

```text
Flexible
FixedColumnCount
FixedRowCount
```

Legal `contentSizeFitter.horizontalFit` and `verticalFit`:

```text
Unconstrained
MinSize
PreferredSize
```

Legal `aspectRatioFitter.aspectMode`:

```text
None
WidthControlsHeight
HeightControlsWidth
FitInParent
EnvelopeParent
```

Hard rules:

- Use at most one layout group on the same node: `horizontalLayout`, `verticalLayout`, or `gridLayout`.
- Do not put `contentSizeFitter` and `aspectRatioFitter` on the same node.
- `gridLayout.cellSize.x` and `gridLayout.cellSize.y` must be greater than zero.
- `FixedColumnCount` and `FixedRowCount` require `constraintCount > 0`.
- Avoid layout group and `contentSizeFitter` on the same node unless intentionally needed.
- Give `Button`, `InputField`, and list item children stable `layoutElement` sizes.
- Give TMP text stable width; list item text should not depend on unresolved stretch alone.

## bindings.json

Field rules:

- Runtime-refreshed text, image, interactable state, input values, and list items need fields.
- List data uses `IReadOnlyList<MessagePayload>`.
- Do not invent business-specific strong model types in Source JSON. Business models stay behind Controller services and are mapped into Store fields or `MessagePayload` items.

Event rules:

| Control | Event | Handler |
|---|---|---|
| Button | `onClick` | `OnXxxRequested` |
| Toggle | `onValueChanged` | `OnXxxChanged` |
| Slider | `onValueChanged` | `OnXxxChanged` |
| InputField | `onEndEdit` | `OnXxxSubmitted` |
| Dropdown | `onValueChanged` | `OnXxxChanged` |
| VerticalList | `onItemClick` | `OnXxxItemClicked` |

Do not make View events write Store directly. Do not use automatic TwoWay binding.

For `VerticalList`, declare the concrete clickable item control in `layout.json` under `verticalList.itemEvents`. Do not also duplicate the same `onItemClick` in top-level `bindings.events`; the generator turns `itemEvents` into the `UIListView.ItemClicked` Controller event contract.

## strings.json

Rules:

- Put static UI copy in `strings.json`.
- Include `zh-Hans` and `en-US` when adding new copy unless the user says otherwise.
- Static layout text should reference `locKey`.
- Do not add parameter-format expressions in this version.

## assets.json

Rules:

- Add assets only when a real source path exists.
- Do not fabricate image, font, sprite, material, or texture paths.
- Source-owned assets live under `Assets/UI/Source/<PackageId>/Assets/`.
- Source-owned assets must target `Assets/UI/Generated/<PackageId>/Assets/...`.
- Shared assets must be under `sharedAssetRoots`.
- `contentHash` is optional in the current static asset strategy. If present, verify `sha256:`. If absent, report a Warning and actual hash, but do not block generation.
- The current static asset strategy supports static `Sprite` references for `Image`; do not imply runtime Addressables asset-key sprite loading.

## codegen.json

Rules:

- Use `<PackageId>View`, `<PackageId>Controller`, and `<PackageId>ViewModel`.
- Default `outputRoot` is `../../Generated/<PackageId>`.
- Default `addressablesKey` is `UI/<PackageId>/<PackageId>View`.
- Generated Controller contains event entry points only. Business logic belongs in handwritten partial code when explicitly requested.
- New UI authoring is UI-first by default. Do not add `requiredServices` unless the user asks to connect external business data.
- If the UI needs data, commands, or change notifications from outside the UI layer during a business connection task, declare `requiredServices`.
- Do not use `requiredServices` for pure UI state such as selected tab, search text, local filters, or close/open events.

Example:

```json
{
  "requiredServices": [
    {
      "type": "Game.Inventory.IInventoryService",
      "property": "InventoryService"
    }
  ]
}
```

`type` is the full C# interface type name. `property` is the protected generated Controller property name. Handwritten Controller partial code may use the generated property to query business data, execute business commands, subscribe to business changes, and map business models into Store fields.

Handwritten Controller partials must be placed at `Assets/UI/<PackageId>/<PackageId>Controller.cs`. Do not create extra `Controllers/`, `Business/`, or `Partial/` subfolders, and do not place handwritten partials under `Assets/UI/Generated/`.

When the handwritten Controller must stay compilable before regenerated scripts exist, it may call `RequireService<T>()` directly in the partial code. The `requiredServices` declaration is still required so generated code resolves the service before partial hooks after Generate.

MVVM-C business access rules:

- Only Controller partial code may access business services.
- View, Binder, UIListView, and MessageBus must not access business services.
- Controller may cache UI state and business ids, but not long-lived copies of business source lists.
- Business model objects should not be passed to View or Binder.
- One UI event or one business notification should batch `Store.Update` calls and `Flush` once.

Business adapter rules:

- Existing business classes such as `PlayerController` are not required services directly.
- Create a UI-facing service interface and one adapter MonoBehaviour in the business directory.
- The adapter registers the interface with `UIManager`.
- If no business exists yet, keep the UI pure and mark Runtime pending; do not create DemoService by default.
