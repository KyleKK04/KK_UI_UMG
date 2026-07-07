# KK_UI_UMG Source Example Patterns

These paths refer to examples in the current development project when they exist. They are authoring references, not files guaranteed to exist after installing the UPM package into a clean Unity project.

For package release checks, do not treat project-level `Assets/UI/Source/...` examples as part of `Packages/com.kk.ui-umg`. Package-owned examples live under `Sample/`.

The package now ships a visible sample:

```text
Sample/InventoryPanelSample
```

For new UI creation, prefer the sample Source folder as the authoring template:

```text
Sample/InventoryPanelSample/Source/KkSampleInventoryPanel
```

Do not use `Sample/InventoryPanelSample/Generated` as an authoring template.

| Example | When to Read |
|---|---|
| `Sample/InventoryPanelSample` | Package-contained end-to-end sample with Source JSON, Generated quick-start output, Controller partial, UIManager bootstrap, and `IInventoryService` |
| `Assets/UI/Source/SimpleMessageBox/` | Minimal dialog, static assets, basic Button / Text / Image |
| `Assets/UI/Source/ConfirmDialog/` | Dialog naming, two-button layout, no business event binding pattern |
| `Assets/UI/Source/InventoryPanel/` | Full component sample, `VerticalList`, Store fields, complex event contract |
| `Assets/UI/Source/LayoutComponentsGallery/` | Legal LayoutComponents syntax and layout component combinations |
| `Assets/UI/Source/QuestPanel/` | AI authoring acceptance package with `VerticalList`, search/category controls, detail panel, and v0.7 Preview verification |

## Common Patterns

Dialog:

```text
Root Panel
├─ Title Text
├─ Body Text / Content
└─ Button Row
   ├─ Confirm Button
   └─ Cancel Button
```

List Panel:

```text
Root Panel
├─ Header / Toolbar
├─ Body
│  ├─ VerticalList / ScrollView
│  └─ Detail Panel
└─ Footer Actions
```

Settings:

```text
Root Panel
├─ Header
├─ Section List
│  ├─ Toggle rows
│  ├─ Slider rows
│  ├─ Dropdown rows
│  └─ Input rows
└─ Footer Actions
```

## Example Requests

Create from natural language:

```text
Create a QuestPanel with a left quest list, right quest details, top search and category controls, and bottom Track and Close buttons.
```

Expected authoring result:

- Create `Assets/UI/Source/QuestPanel/`.
- Use `VerticalList` for quests.
- Use LayoutComponents for main panel layout.
- Add Store fields for selected quest title, description, reward, filter state, and list items.
- Add events for search submit, category change, quest item click, track request, and close request.
- Add `README.md` and `validation.md` with v0.7.1 ledger markers.

Modify existing UI:

```text
Add a "usable only" Toggle to InventoryPanel's filter bar.
```

Expected authoring result:

- Read all InventoryPanel Source manifests first.
- Add Toggle layout node.
- Add static strings for label.
- Add field and binding for current value.
- Add `OnUsableOnlyChanged` event contract.
- Do not edit Generated or handwritten business Controller unless explicitly requested.
- Let the pipeline update the `validation.md` ledger after Validate / Generate / Verify / Preview; do not save screenshots.

Business-backed UI:

```text
Make InventoryPanel read real inventory items from IInventoryService.
```

Expected authoring result:

- Do not add `InventoryItemModel` or other business model types to `bindings.json`.
- Keep list display data as `IReadOnlyList<MessagePayload>`.
- Add `codegen.requiredServices` with `type: Game.Inventory.IInventoryService` and `property: InventoryService`.
- Put handwritten Controller partial code at `Assets/UI/Generated/InventoryPanel/InventoryPanelController.cs` and map service item models into Store fields there.
- Do not make View, Binder, UIListView, or MessageBus access `IInventoryService`.

Existing business adapter:

```text
Connect PlayerHud to the existing PlayerController health and mana data.
```

Expected authoring result:

- Read the PlayerHud Source package and the existing PlayerController.
- Create `IPlayerHudService` and `PlayerHudServiceAdapter` in the player business directory.
- Add only the minimum public getters/events to PlayerController if they are missing.
- Add `codegen.requiredServices` for `IPlayerHudService`.
- Put or update `Assets/UI/Generated/PlayerHud/PlayerHudController.cs` to subscribe to service changes, map snapshots to Store fields, and flush once.
