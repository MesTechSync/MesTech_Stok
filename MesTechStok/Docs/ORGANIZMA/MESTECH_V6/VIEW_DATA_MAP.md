# VIEW_DATA_MAP — Avalonia View Veri Bağımlılık Haritası
# Tarih: 2026-03-31 12:57 | DEV 2 | Katman 1.5 scope

## Özet
| Kategori | Sayı | Açıklama |
|----------|------|----------|
| VERİ_GEREKLİ (query bağlı) | 150 | DataGrid/ListBox + MediatR Send |
| VERİ_GEREKLİ (orphan) | 3 | ItemsSource var ama query yok (Settings/Welcome) |
| FORM_ONLY | 15 | Settings/Create formları |
| STATIC | 4 | About/Welcome/Login |
| **TOPLAM** | **172** | |

## Seed Data Gerekli Entity'ler (view'lardan çıkarılan)
| Entity | Kullanan View Sayısı | Örnek View |
|--------|---------------------|------------|
| Product | ~20 | Products, Stock, BulkProduct, Buybox |
| Order | ~15 | Orders, OrderDetail, Dashboard, Dropship |
| Customer | ~8 | Customer, Contacts, CRM Dashboard |
| Invoice/EInvoice | ~10 | EInvoice, InvoiceList, InvoiceReport |
| StockMovement | ~5 | Stock, StockMovement, StockTimeline |
| Category | ~5 | Category, CategoryMapping |
| Supplier | ~3 | Supplier, SupplierFeedList |
| Warehouse | ~3 | Warehouse, WarehouseSummary |
| GLAccount/JournalEntry | ~8 | AccountingDashboard, TrialBalance, KarZarar |
| Cargo/Shipment | ~5 | Cargo, CargoTracking, BulkShipment |
| Notification | ~2 | Notification |
| PlatformMapping | ~15 | Trendyol, Amazon, HB, N11, eBay... |

## VERİ_GEREKLİ View → Query Haritası

- **CommissionRatesView** → GetPlatformCommissionRatesQuery,
- **FixedAssetAvaloniaView** → GetFixedAssetsQuery,
- **FixedExpenseAvaloniaView** → GetFixedExpensesQuery,
- **IncomeExpenseDashboardView** → GetIncomeExpenseListQuery,GetIncomeExpenseSummaryQuery,
- **IncomeExpenseListView** → GetIncomeExpenseListQuery,
- **JournalEntryListView** → GetJournalEntriesQuery,
- **PenaltyAvaloniaView** → GetPenaltyRecordsQuery,
- **ProfitabilityReportView** → ProfitabilityReportQuery,
- **TaxRecordAvaloniaView** → GetTaxRecordsQuery,
- **TrialBalanceView** → GetTrialBalanceQuery,
- **AccountingDashboardAvaloniaView** → GetMonthlySummaryQuery,
- **ActivityAvaloniaView** → _(orphan — query yok)_
- **AmazonAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **AmazonEuAvaloniaView** → GetPlatformDashboardQuery,
- **AppHubView** → _(orphan — query yok)_
- **AuditLogAvaloniaView** → GetAuditLogsQuery,
- **BackupAvaloniaView** → GetBackupHistoryQuery,
- **BarcodeAvaloniaView** → GetBarcodeScanLogsQuery,
- **BarcodeReaderView** → _(orphan — query yok)_
- **BarcodeScannerView** → _(orphan — query yok)_
- **BillingAvaloniaView** → GetBillingInvoicesQuery,GetTenantSubscriptionQuery,
- **Bitrix24AvaloniaView** → _(orphan — query yok)_
- **BordroAvaloniaView** → GetEmployeesQuery,
- **BudgetAvaloniaView** → GetBudgetSummaryQuery,
- **BulkInvoiceAvaloniaView** → BulkCreateInvoiceCommand,GetOrderListQuery,
- **BulkProductAvaloniaView** → BulkUpdateProductsCommand,
- **BulkShipmentAvaloniaView** → CreateShipmentCommand,
- **BuyboxAvaloniaView** → GetBuyboxStatusQuery,GetTopProductsQuery,
- **CalendarAvaloniaView** → _(orphan — query yok)_
- **CampaignAvaloniaView** → CreateCampaignCommand,GetActiveCampaignsQuery,
- **CargoAvaloniaView** → GetCargoTrackingListQuery,
- **CargoProvidersAvaloniaView** → _(orphan — query yok)_
- **CargoSettingsAvaloniaView** → _(orphan — query yok)_
- **CargoTrackingAvaloniaView** → GetCargoTrackingListQuery,
- **CariAvaloniaView** → GetCounterpartiesQuery,
- **CariHesaplarAvaloniaView** → GetCariHesaplarQuery,
- **CategoryAvaloniaView** → GetCategoriesQuery,
- **CategoryMappingAvaloniaView** → _(orphan — query yok)_
- **CiceksepetiAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **ContactAvaloniaView** → GetContactsPagedQuery,
- **CrmDashboardAvaloniaView** → _(orphan — query yok)_
- **CrmSettingsAvaloniaView** → GetCrmSettingsQuery,SaveCrmSettingsCommand,
- **CustomerAvaloniaView** → _(orphan — query yok)_
- **DashboardAvaloniaView** → GetDashboardSummaryQuery,
- **DealsAvaloniaView** → GetDealsQuery,
- **DepartmentAvaloniaView** → _(orphan — query yok)_
- **DocumentFolderAvaloniaView** → _(orphan — query yok)_
- **DocumentManagerAvaloniaView** → GetDocumentsQuery,
- **DropshipDashboardAvaloniaView** → GetDropshipDashboardQuery,
- **DropshipOrdersAvaloniaView** → _(orphan — query yok)_
- **DropshipProfitAvaloniaView** → GetDropshipProfitabilityQuery,
- **EbayAvaloniaView** → GetPlatformDashboardQuery,TriggerSyncCommand,
- **EInvoiceAvaloniaView** → GetEInvoicesQuery,
- **ErpAccountMappingView** → GetErpAccountMappingsQuery,
- **ErpDashboardView** → GetErpDashboardQuery,GetErpSyncLogsQuery,
- **ErpSettingsAvaloniaView** → _(orphan — query yok)_
- **EtsyAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **ExportAvaloniaView** → ExportOrdersCommand,ExportProductsCommand,ExportStockCommand,
- **FeedCreateAvaloniaView** → _(orphan — query yok)_
- **FeedPreviewAvaloniaView** → PreviewFeedCommand,
- **FulfillmentDashboardView** → GetFulfillmentInventoryQuery,GetFulfillmentOrdersQuery,
- **FulfillmentInboundView** → GetFulfillmentOrdersQuery,
- **FulfillmentInventoryView** → GetFulfillmentInventoryQuery,
- **GelirGiderAvaloniaView** → GetIncomeExpenseListQuery,
- **GLTransactionAvaloniaView** → GetJournalEntriesQuery,
- **HealthAvaloniaView** → _(orphan — query yok)_
- **HepsiburadaAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **ImportProductsAvaloniaView** → _(orphan — query yok)_
- **ImportSettingsAvaloniaView** → _(orphan — query yok)_
- **InventoryAvaloniaView** → _(orphan — query yok)_
- **InvoiceCreateAvaloniaView** → _(orphan — query yok)_
- **InvoiceListAvaloniaView** → _(orphan — query yok)_
- **InvoiceManagementAvaloniaView** → GetInvoicesQuery,
- **InvoiceProviderSettingsAvaloniaView** → _(orphan — query yok)_
- **InvoiceReportAvaloniaView** → ExportInvoiceReportCommand,GetInvoiceReportQuery,
- **InvoiceSettingsAvaloniaView** → GetInvoiceSettingsQuery,
- **KanbanAvaloniaView** → _(orphan — query yok)_
- **KanbanBoardAvaloniaView** → _(orphan — query yok)_
- **KarlilikAnaliziAvaloniaView** → GetProfitReportQuery,
- **KarZararAvaloniaView** → GetKarZararQuery,
- **KdvRaporAvaloniaView** → GetKdvReportQuery,
- **KomisyonAvaloniaView** → GetCommissionSummaryQuery,
- **LabelPreviewAvaloniaView** → DownloadShipmentLabelQuery,PrintShipmentLabelCommand,
- **LeadsAvaloniaView** → GetLeadsQuery,
- **LogViewerAvaloniaView** → _(orphan — query yok)_
- **MesaAvaloniaView** → _(orphan — query yok)_
- **StaleOrdersAvaloniaView** → GetStaleOrdersQuery,
- **MultiTenantAvaloniaView** → GetTenantsQuery,
- **MutabakatAvaloniaView** → GetReconciliationDashboardQuery,
- **N11AvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **NakitAkisAvaloniaView** → GetCashFlowReportQuery,
- **NotificationAvaloniaView** → GetUserNotificationsQuery,
- **NotificationSettingsAvaloniaView** → _(orphan — query yok)_
- **OnboardingWizardAvaloniaView** → CompleteOnboardingStepCommand,GetOnboardingProgressQuery,RegisterTenantCommand,
- **OpenCartAvaloniaView** → GetPlatformDashboardQuery,GetStoresByTenantQuery,SyncPlatformCommand,TestStoreConnectionCommand,
- **OrderDetailAvaloniaView** → GetOrderListQuery,
- **OrderListAvaloniaView** → GetOrderListQuery,
- **OrderKanbanView** → GetOrdersByStatusQuery,
- **OrdersAvaloniaView** → GetOrderListQuery,
- **OzonAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **PazaramaAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **PipelineAvaloniaView** → _(orphan — query yok)_
- **PlatformConnectionTestAvaloniaView** → _(orphan — query yok)_
- **PlatformListAvaloniaView** → _(orphan — query yok)_
- **PlatformMessagesAvaloniaView** → _(orphan — query yok)_
- **PlatformSyncAvaloniaView** → GetPlatformSyncStatusQuery,SyncPlatformCommand,
- **PlatformSyncHistoryAvaloniaView** → GetSyncHistoryQuery,TriggerSyncCommand,
- **PlatformSyncStatusAvaloniaView** → _(orphan — query yok)_
- **ProductDescriptionAIView** → _(orphan — query yok)_
- **ProductsAvaloniaView** → _(orphan — query yok)_
- **ProductVariantMatrixView** → _(orphan — query yok)_
- **ProfitLossAvaloniaView** → GetProfitReportQuery,
- **ProjectsAvaloniaView** → GetProjectsQuery,
- **PttAvmAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **QuotationAvaloniaView** → ListQuotationsQuery,
- **ReportAvaloniaView** → _(orphan — query yok)_
- **ReportDashboardAvaloniaView** → _(orphan — query yok)_
- **CashFlowReportView** → GetCashFlowReportQuery,
- **SalesAnalyticsView** → GetSalesAnalyticsQuery,
- **StockValueReportView** → GetStockValueReportQuery,
- **ReturnDetailAvaloniaView** → ApproveReturnCommand,GetReturnListQuery,RejectReturnCommand,
- **ReturnListAvaloniaView** → GetReturnListQuery,
- **SabitGiderlerAvaloniaView** → GetFixedExpensesQuery,
- **SettingsAvaloniaView** → GetCredentialsSettingsQuery,GetStoresByTenantQuery,SaveApiSettingsCommand,TestApiConnectionCommand,TestStoreConnectionCommand,
- **SettlementAvaloniaView** → GetSettlementBatchesQuery,
- **ShipmentAvaloniaView** → GetShipmentCostsQuery,
- **ShopifyAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **StockAlertAvaloniaView** → _(orphan — query yok)_
- **StockLotAvaloniaView** → _(orphan — query yok)_
- **StockMovementAvaloniaView** → BulkUpdateStockCommand,GetStockMovementsQuery,
- **StockPlacementAvaloniaView** → _(orphan — query yok)_
- **StockTimelineAvaloniaView** → _(orphan — query yok)_
- **StockTransferAvaloniaView** → _(orphan — query yok)_
- **StockUpdateAvaloniaView** → BulkUpdateStockCommand,GetProductsQuery,
- **StoreDetailAvaloniaView** → GetStoreDetailQuery,GetStoresByTenantQuery,
- **StoreManagementAvaloniaView** → GetStoreSettingsQuery,
- **StoreWizardAvaloniaView** → _(orphan — query yok)_
- **SupplierAvaloniaView** → GetSuppliersCrmQuery,
- **SupplierFeedListAvaloniaView** → GetFeedSourcesQuery,
- **SyncStatusAvaloniaView** → _(orphan — query yok)_
- **TimeEntryAvaloniaView** → CreateTimeEntryCommand,GetTimeEntriesQuery,
- **TransferWizardAvaloniaView** → GetWarehousesQuery,
- **TrendyolAvaloniaView** → GetPlatformDashboardQuery,GetProductsQuery,GetStoresByTenantQuery,TestStoreConnectionCommand,
- **UserManagementAvaloniaView** → GetUsersQuery,
- **VergiTakvimiAvaloniaView** → GetTaxRecordsQuery,
- **WarehouseAvaloniaView** → _(orphan — query yok)_
- **WarehouseSummaryAvaloniaView** → _(orphan — query yok)_
- **WelcomeAvaloniaView** → _(orphan — query yok)_
- **WelcomeWindow** → _(orphan — query yok)_
- **WooCommerceAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
- **WorkScheduleAvaloniaView** → GetEmployeesQuery,
- **WorkTaskAvaloniaView** → GetProjectTasksQuery,
- **ZalandoAvaloniaView** → GetPlatformDashboardQuery,SyncPlatformCommand,
