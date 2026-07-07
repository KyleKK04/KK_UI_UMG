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

## Run

Use the menu:

```text
KK_UI_UMG/Sample/Open Inventory Panel Sample
```

The menu registers the package prefab as Addressable with:

```text
UI/KkSampleInventoryPanel/KkSampleInventoryPanelView
```

Then it opens:

```text
Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Scene/KkSampleInventorySample.unity
```

Enter Play Mode. The scene has a `UIManager`, demo `IInventoryService`, and bootstrap that calls:

```csharp
await UIManager.Instance.OpenAsync("KkSampleInventoryPanel");
```
