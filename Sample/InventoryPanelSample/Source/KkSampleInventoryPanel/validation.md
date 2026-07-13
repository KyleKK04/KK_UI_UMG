# KkSampleInventoryPanel Validation

<!-- ui-pipeline:validation-ledger:start -->
## Pipeline Ledger

| Step | Status | Updated At | Source | Notes |
|---|---|---|---|---|
| Validate | Pass | 2026-07-13T12:32:02.2085310Z | KKUIPipeline | - |
| Generate | Pass | 2026-07-13T12:32:02.0477600Z | KKUIPipeline | - |
| Verify | Pass | 2026-07-13T12:32:02.2085310Z | GeneratedAssetVerifier | - |
| Preview | Pass | 2026-07-07T06:06:18.9971350Z | KKUIPipelineWindow | - |
| Runtime | Pending | 2026-07-13T12:32:02.0477600Z | KKUIPipeline | Generate requires runtime re-verification. |

## Last Operation

| Item | Value |
|---|---|
| Operation | Verify |
| Success | True |
| Issues | 0 |
| Generated Report | `Packages/com.kk.ui-umg/Sample/InventoryPanelSample/Generated/KkSampleInventoryPanel/Reports/generate-report.json` |
<!-- ui-pipeline:validation-ledger:end -->

## Manual Notes

- Static UI copy uses `locKey + strings.json` and does not create Store fields.
- Runtime-changing item details, status, filters, button state, and list data are driven by Controller -> Store -> Binder.
- Runtime requires `KK.UI.UMG.Samples.Inventory.IInventoryService` to be registered before `OpenAsync("KkSampleInventoryPanel")`.
