# KkSampleInventoryPanel Validation

<!-- ui-pipeline:validation-ledger:start -->
## Pipeline Ledger

| Step | Status | Updated At | Source | Notes |
|---|---|---|---|---|
| Validate | Pass | 2026-07-07T06:06:17.6552570Z | KKUIPipeline | - |
| Generate | Pass | 2026-07-07T06:06:16.2366180Z | KKUIPipeline | - |
| Verify | Pass | 2026-07-07T06:06:17.6552570Z | GeneratedAssetVerifier | - |
| Preview | Pass | 2026-07-07T06:06:18.9971350Z | KKUIPipelineWindow | - |
| Runtime | Pending | - | Manual | Open `Assets/Scenes/KkSampleInventorySample.unity` and Play after installing the sample. |

## Last Operation

| Item | Value |
|---|---|
| Operation | Preview |
| Success | True |
| Issues | 0 |
| Generated Report | `Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Generated/KkSampleInventoryPanel/Reports/generate-report.json` |
<!-- ui-pipeline:validation-ledger:end -->

## Manual Notes

- Static UI copy uses `locKey + strings.json` and does not create Store fields.
- Runtime-changing item details, status, filters, button state, and list data are driven by Controller -> Store -> Binder.
- Runtime requires `KK.UI.UMG.Samples.Inventory.IInventoryService` to be registered before `OpenAsync("KkSampleInventoryPanel")`.
