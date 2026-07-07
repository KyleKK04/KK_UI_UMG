# Inventory Panel Sample

`KkSampleInventoryPanel` is a package-contained KK_UI_UMG sample. It demonstrates Source JSON, generated UGUI output, a generated prefab, `UIManager.OpenAsync`, a Controller partial, service registration, and runtime business updates.

## Layout

```text
Packages/com.kk.ui-umg/Sample/InventoryPanelSample/
├─ Source/KkSampleInventoryPanel/
├─ Generated/KkSampleInventoryPanel/
├─ Scripts/Inventory/
└─ Scene/KkSampleInventorySample.unity
```

The handwritten Controller partial lives at:

```text
Generated/KkSampleInventoryPanel/KkSampleInventoryPanelController.cs
```

The generated-owned C# output remains under `Generated/KkSampleInventoryPanel/Scripts/`.

The Source files are included as reference material. In a real project, create or edit Source packages under:

```text
Assets/UI/Source/<PackageId>/
```

Generated output in this sample is included so the scene can run directly from the package.

## Use As Template

For new UI packages, use this sample as the primary authoring reference. Ask Codex / the `kk-ui-umg` skill to read:

```text
Sample/InventoryPanelSample/Source/KkSampleInventoryPanel
```

Then generate a project-owned Source package under:

```text
Assets/UI/Source/<PackageId>
```

Do not hand-copy or hand-edit the sample `Generated/` output as a template. Generated files are quick-start artifacts and can be rebuilt from Source JSON.

## Run

The sample scene is:

```text
Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Scene/KkSampleInventorySample.unity
```

The generated prefab is expected to be Addressable with:

```text
UI/KkSampleInventoryPanel/KkSampleInventoryPanelView
```

Enter Play Mode. The scene has a `UIManager`, demo `IInventoryService`, and bootstrap that calls:

```csharp
await UIManager.Instance.OpenAsync("KkSampleInventoryPanel");
```
