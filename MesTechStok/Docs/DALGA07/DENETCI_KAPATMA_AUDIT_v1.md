# Denetci Kapatma Audit Report v1

> **DEV 1 — Dalga 7, Batch 2 (Tasks 9-11)**
> **Date:** 2026-03-12
> **Status:** DEV 1 scope COMPLETE — DEV 2 handoff items documented

---

## DEV 1 Completed Work

### Task 9: Static App.ServiceProvider Elimination (Services + ViewModels)

| File | Refs Removed | Fix |
|------|-------------|-----|
| BarcodeHardwareService.cs | 3 | IServiceScopeFactory + IConfiguration ctor injection |
| EnhancedProductService.cs | 1 | ILoggingService? ctor injection (D-11 pattern) |
| SqlBackedProductService.cs | 1 | IOfflineQueueService? ctor injection (D-11 pattern) |
| LogCommandViewModel.cs | 1 | IServiceProvider? ctor injection + ??= fallback |
| **Total** | **6** | |

**Post-fix count:**
- `App.ServiceProvider` in Services/: **0** (was 6)
- `App.ServiceProvider` in ViewModels/: **1** (LogCommandViewModel fallback — acceptable D-11 pattern)

### Task 10: BarcodeScannerService Core.AppDbContext Decoupling

- **Before:** Line 626 directly resolved `Core.Data.AppDbContext` via `IServiceScopeFactory`
- **After:** `Action<BarcodeScanLogData>` delegate injected via constructor; Desktop DI wires MediatR `CreateBarcodeScanLogCommand`
- **New file:** `MesTechStok.Core/Integrations/Barcode/Models/BarcodeScanLogData.cs` (lightweight DTO)
- `Core.Data.AppDbContext` refs in `MesTechStok.Core`: **0 new** (BarcodeScannerService cleaned)

---

## DEV 2 Handoff: Remaining Anti-Pattern References

### A. `App.ServiceProvider` in Views (87 refs across 22 files)

**Golden Rule: These are existing Views — DO NOT break them. Convert incrementally.**

| View File | Refs | Priority | Notes |
|-----------|------|----------|-------|
| ProductsView.xaml.cs | 14 | P1 | Largest — multiple service resolutions |
| BarcodeView.xaml.cs | 12 | P1 | DB + service heavy |
| ProductUploadPopup.xaml.cs | 6 | P2 | Image storage + adapter |
| SettingsView.xaml.cs | 6 | P2 | Config + theme |
| MainWindow.xaml.cs | 6 | P2 | App bootstrap — careful |
| InventoryView.xaml.cs | 4 | P2 | Includes AppDbContext |
| WelcomeWindow.xaml.cs | 4 | P3 | System check + loading |
| CustomersView.xaml.cs | 4 | P3 | CRUD operations |
| DashboardView.xaml.cs | 3 | P3 | Chart + stats |
| LogView.xaml.cs | 3 | P3 | Includes AppDbContext |
| ReportsView.xaml.cs | 3 | P3 | PDF generation |
| SettingsOverlayWindow.xaml.cs | 1 | P4 | |
| ImageMapWizard.xaml.cs | 2 | P4 | Includes AppDbContext |
| PlatformOrdersView.xaml.cs | 2 | P4 | |
| TrendyolConnectionView.xaml.cs | 2 | P4 | |
| ProductEditDialog.xaml.cs | 2 | P4 | |
| OrdersView.xaml.cs | 1 | P4 | |
| LoginWindow.xaml.cs | 1 | P4 | |
| CategoryManagerDialog.xaml.cs | 1 | P4 | |
| ProductImportWizard.xaml.cs | 1 | P4 | |
| ProductUploadPopup_Enhanced.xaml.cs | 1 | P4 | |
| SimpleTestView.xaml.cs | 1 | P4 | |
| SystemResourcesView.xaml.cs | 1 | P4 | |
| TelemetryView.xaml.cs | 2 | P4 | |
| PlatformSyncStatusView.xaml.cs | 1 | P4 | |

**Pattern to follow:** D-11 optional ctor params (`T? service = null`) with `??= App.ServiceProvider?.GetService<T>()` fallback for WPF designer compatibility. See InvoiceManagementView, InvoiceSettingsView, BulkCargoLabelDialog for reference.

### B. `Core.Data.AppDbContext` Direct Usage in Views (5 refs in 4 files)

| View File | Line | Usage |
|-----------|------|-------|
| BarcodeView.xaml.cs | 1587 | `scope.ServiceProvider.GetService<AppDbContext>()` — scan log persist |
| ImageMapWizard.xaml.cs | 112, 150 | Product image mapping |
| InventoryView.xaml.cs | 110 | `GetRequiredService<AppDbContext>()` — inventory queries |
| LogView.xaml.cs | 831 | `scope.ServiceProvider.GetService<AppDbContext>()` — log queries |

**Fix pattern:** Replace with MediatR commands/queries (already exist from Dalga 6 Batch 1):
- BarcodeView → `CreateBarcodeScanLogCommand`
- ImageMapWizard → `UpdateProductImageCommand`
- InventoryView → `GetInventoryPagedQuery` / `GetInventoryStatisticsQuery`
- LogView → `GetBarcodeScanLogsQuery`

### C. `AppDbContext` in App.xaml.cs (backward compat registration)

```
App.xaml.cs:364: #pragma warning disable CS0618 // Obsolete Core.AppDbContext — intentional backward compat
```

This `[Obsolete]` registration must stay until all View references in Section B are converted. Remove it last.

---

## Summary Metrics

| Metric | Before Dalga 7 | After DEV 1 | DEV 2 Target |
|--------|----------------|-------------|--------------|
| App.ServiceProvider in Services | 6 | **0** | 0 |
| App.ServiceProvider in ViewModels | 1 (fallback) | **1** (D-11) | 0 |
| App.ServiceProvider in Views | 87 | 87 | 0 |
| Core.AppDbContext in BarcodeScannerService | 1 | **0** | 0 |
| Core.AppDbContext in Views | 5 | 5 | 0 |

---

## Commits

| Hash | Description |
|------|-------------|
| `2f886b4` | Task 9: Eliminate 6 static App.ServiceProvider refs (Services+ViewModels) |
| `945e2c9` | Task 10: BarcodeScannerService AppDbContext → MediatR delegate |
