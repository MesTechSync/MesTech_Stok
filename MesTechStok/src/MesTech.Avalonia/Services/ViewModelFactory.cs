using CommunityToolkit.Mvvm.ComponentModel;
using MesTech.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using AcctVm = MesTech.Avalonia.ViewModels.Accounting;
using ErpVm = MesTech.Avalonia.ViewModels.Erp;
using MonVm = MesTech.Avalonia.ViewModels.Monitoring;
using OrdVm = MesTech.Avalonia.ViewModels.Orders;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Concrete ViewModel factory backed by IServiceScopeFactory.
/// P0 FIX: Each navigation creates a NEW DI scope so that
/// Scoped services (DbContext, repositories, UnitOfWork) get
/// a fresh instance per view — prevents DbContext threading crashes.
/// Previous scope is disposed when a new view is navigated to.
/// </summary>
public sealed class ViewModelFactory : IViewModelFactory, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope? _currentScope;

    public ViewModelFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public ObservableObject? Create(string viewName)
    {
        // Dispose previous scope — releases old DbContext + repos
        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();
        var sp = _currentScope.ServiceProvider;

        return viewName switch
        {
            // Core views (Dalga 10)
            "AppHub" => sp.GetService<AppHubViewModel>(),
            "MfaSetup" => sp.GetService<MfaSetupViewModel>(),
            "Bitrix24" => sp.GetService<Bitrix24AvaloniaViewModel>(),
            "Campaign" => sp.GetService<CampaignAvaloniaViewModel>(),
            "Dashboard" => sp.GetService<DashboardAvaloniaViewModel>(),
            "Leads" => sp.GetService<LeadsAvaloniaViewModel>(),
            "Kanban" => sp.GetService<KanbanAvaloniaViewModel>(),
            "ProfitLoss" => sp.GetService<ProfitLossAvaloniaViewModel>(),
            "Products" => sp.GetService<ProductsAvaloniaViewModel>(),
            "Stock" => sp.GetService<StockAvaloniaViewModel>(),
            "Orders" => sp.GetService<OrdersAvaloniaViewModel>(),
            "Settings" => sp.GetService<SettingsAvaloniaViewModel>(),
            "Login" => sp.GetService<LoginAvaloniaViewModel>(),
            "Category" => sp.GetService<CategoryAvaloniaViewModel>(),
            // Dalga 11 batch expansion
            "Contacts" => sp.GetService<ContactsAvaloniaViewModel>(),
            "Employees" => sp.GetService<EmployeesAvaloniaViewModel>(),
            "LeaveRequests" => sp.GetService<LeaveRequestsAvaloniaViewModel>(),
            "Documents" => sp.GetService<DocumentsAvaloniaViewModel>(),
            "Reports" => sp.GetService<ReportsAvaloniaViewModel>(),
            "Marketplaces" => sp.GetService<MarketplacesAvaloniaViewModel>(),
            "Expenses" => sp.GetService<ExpensesAvaloniaViewModel>(),
            "BankAccounts" => sp.GetService<BankAccountsAvaloniaViewModel>(),
            // Dalga 14+15 functional views
            "Inventory" => sp.GetService<InventoryAvaloniaViewModel>(),
            "InvoiceManagement" => sp.GetService<InvoiceManagementAvaloniaViewModel>(),
            "Customers" => sp.GetService<CustomerAvaloniaViewModel>(),
            "CariHesaplar" => sp.GetService<CariHesaplarAvaloniaViewModel>(),
            "SyncStatus" => sp.GetService<SyncStatusAvaloniaViewModel>(),
            "StockMovement" => sp.GetService<StockMovementAvaloniaViewModel>(),
            "CargoTracking" => sp.GetService<CargoTrackingAvaloniaViewModel>(),
            // Dalga 17 batch (T007)
            "About" => sp.GetService<AboutAvaloniaViewModel>(),
            "AccountingDashboard" => sp.GetService<AccountingDashboardAvaloniaViewModel>(),
            "Activity" => sp.GetService<ActivityAvaloniaViewModel>(),
            "Amazon" => sp.GetService<AmazonAvaloniaViewModel>(),
            "AmazonEu" => sp.GetService<AmazonEuAvaloniaViewModel>(),
            "Barcode" => sp.GetService<BarcodeAvaloniaViewModel>(),
            "BarcodeScanner" => sp.GetService<BarcodeScannerViewModel>(),
            "Calendar" => sp.GetService<CalendarAvaloniaViewModel>(),
            "CargoSettings" => sp.GetService<CargoSettingsAvaloniaViewModel>(),
            "Ciceksepeti" => sp.GetService<CiceksepetiAvaloniaViewModel>(),
            "Contact" => sp.GetService<ContactAvaloniaViewModel>(),
            "CrmDashboard" => sp.GetService<CrmDashboardAvaloniaViewModel>(),
            "PlatformMessages" => sp.GetService<PlatformMessagesAvaloniaViewModel>(),
            "CrmSettings" => sp.GetService<CrmSettingsAvaloniaViewModel>(),
            "Deals" => sp.GetService<DealsAvaloniaViewModel>(),
            "Department" => sp.GetService<DepartmentAvaloniaViewModel>(),
            "DocumentFolder" => sp.GetService<DocumentFolderAvaloniaViewModel>(),
            "DocumentManager" => sp.GetService<DocumentManagerAvaloniaViewModel>(),
            "Ebay" => sp.GetService<EbayAvaloniaViewModel>(),
            "ErpSettings" => sp.GetService<ErpSettingsAvaloniaViewModel>(),
            "GLTransaction" => sp.GetService<GLTransactionAvaloniaViewModel>(),
            "GelirGider" => sp.GetService<GelirGiderAvaloniaViewModel>(),
            "Health" => sp.GetService<HealthAvaloniaViewModel>(),
            "Hepsiburada" => sp.GetService<HepsiburadaAvaloniaViewModel>(),
            "InvoiceSettings" => sp.GetService<InvoiceSettingsAvaloniaViewModel>(),
            "KanbanBoard" => sp.GetService<KanbanBoardAvaloniaViewModel>(),
            "KarZarar" => sp.GetService<KarZararAvaloniaViewModel>(),
            "Mesa" => sp.GetService<MesaAvaloniaViewModel>(),
            "MultiTenant" => sp.GetService<MultiTenantAvaloniaViewModel>(),
            "Mutabakat" => sp.GetService<MutabakatAvaloniaViewModel>(),
            "N11" => sp.GetService<N11AvaloniaViewModel>(),
            "Notification" => sp.GetService<NotificationAvaloniaViewModel>(),
            "OpenCart" => sp.GetService<OpenCartAvaloniaViewModel>(),
            "OrderDetail" => sp.GetService<OrderDetailAvaloniaViewModel>(),
            "OrderList" => sp.GetService<OrderListAvaloniaViewModel>(),
            "Ozon" => sp.GetService<OzonAvaloniaViewModel>(),
            "Etsy" => sp.GetService<EtsyAvaloniaViewModel>(),
            "Shopify" => sp.GetService<ShopifyAvaloniaViewModel>(),
            "WooCommerce" => sp.GetService<WooCommerceAvaloniaViewModel>(),
            "Zalando" => sp.GetService<ZalandoAvaloniaViewModel>(),
            "Pazarama" => sp.GetService<PazaramaAvaloniaViewModel>(),
            "Pipeline" => sp.GetService<PipelineAvaloniaViewModel>(),
            "Projects" => sp.GetService<ProjectsAvaloniaViewModel>(),
            "PttAvm" => sp.GetService<PttAvmAvaloniaViewModel>(),
            "Report" => sp.GetService<ReportAvaloniaViewModel>(),
            "Settlement" => sp.GetService<SettlementAvaloniaViewModel>(),
            "Shipment" => sp.GetService<ShipmentAvaloniaViewModel>(),
            "StoreManagement" => sp.GetService<StoreManagementAvaloniaViewModel>(),
            "Supplier" => sp.GetService<SupplierAvaloniaViewModel>(),
            "Tenant" => sp.GetService<TenantAvaloniaViewModel>(),
            "TimeEntry" => sp.GetService<TimeEntryAvaloniaViewModel>(),
            "Trendyol" => sp.GetService<TrendyolAvaloniaViewModel>(),
            "UserManagement" => sp.GetService<UserManagementAvaloniaViewModel>(),
            "Warehouse" => sp.GetService<WarehouseAvaloniaViewModel>(),
            // EMR-06 Gorev 4B: Stok Yerlesim + Lot + Transfer
            "StockPlacement" => sp.GetService<StockPlacementAvaloniaViewModel>(),
            "StockLot" => sp.GetService<StockLotAvaloniaViewModel>(),
            "StockTransfer" => sp.GetService<StockTransferAvaloniaViewModel>(),
            // Invoice views (e-Fatura batch)
            "InvoiceList" => sp.GetService<InvoiceListAvaloniaViewModel>(),
            "InvoiceCreate" => sp.GetService<InvoiceCreateAvaloniaViewModel>(),
            "BulkInvoice" => sp.GetService<BulkInvoiceAvaloniaViewModel>(),
            "InvoicePdf" => sp.GetService<InvoicePdfAvaloniaViewModel>(),
            "InvoiceProviders" => sp.GetService<InvoiceProviderSettingsAvaloniaViewModel>(),
            "InvoiceReport" => sp.GetService<InvoiceReportAvaloniaViewModel>(),
            "Welcome" => sp.GetService<WelcomeAvaloniaViewModel>(),
            "WorkSchedule" => sp.GetService<WorkScheduleAvaloniaViewModel>(),
            "WorkTask" => sp.GetService<WorkTaskAvaloniaViewModel>(),
            // EMR-10 Platform + Dropshipping views
            "PlatformList" => sp.GetService<PlatformListAvaloniaViewModel>(),
            "StoreWizard" => sp.GetService<StoreWizardAvaloniaViewModel>(),
            "CategoryMapping" => sp.GetService<CategoryMappingAvaloniaViewModel>(),
            "DropshipDashboard" => sp.GetService<DropshipDashboardAvaloniaViewModel>(),
            "FeedPreview" => sp.GetService<FeedPreviewAvaloniaViewModel>(),
            "StoreSettings" => sp.GetService<StoreSettingsAvaloniaViewModel>(),
            "StoreDetail" => sp.GetService<StoreDetailAvaloniaViewModel>(),
            "ProductFetch" => sp.GetService<ProductFetchAvaloniaViewModel>(),
            "ProductDescriptionAI" => sp.GetService<ProductDescriptionAIViewModel>(),
            "SupplierFeeds" => sp.GetService<SupplierFeedListAvaloniaViewModel>(),
            "FeedCreate" => sp.GetService<FeedCreateAvaloniaViewModel>(),
            "DropshipOrders" => sp.GetService<DropshipOrdersAvaloniaViewModel>(),
            "DropshipProfit" => sp.GetService<DropshipProfitAvaloniaViewModel>(),
            "DropshippingPool" => sp.GetService<DropshippingPoolAvaloniaViewModel>(),
            "Cheque" => sp.GetService<ChequeAvaloniaViewModel>(),
            "CommissionCompare" => sp.GetService<CommissionCompareAvaloniaViewModel>(),
            "PerformanceDashboard" => sp.GetService<PerformanceDashboardAvaloniaViewModel>(),
            "Customer360" => sp.GetService<Customer360AvaloniaViewModel>(),
            "CashRegister" => sp.GetService<CashRegisterAvaloniaViewModel>(),
            "NewOrder" => sp.GetService<NewOrderAvaloniaViewModel>(),
            "ImportSettings" => sp.GetService<ImportSettingsAvaloniaViewModel>(),
            "ImportProducts" => sp.GetService<ImportProductsAvaloniaViewModel>(),
            "ProductVariantMatrix" => sp.GetService<ProductVariantMatrixViewModel>(),
            "PlatformSyncStatus" => sp.GetService<PlatformSyncStatusAvaloniaViewModel>(),
            // İ-05 Siparis/Kargo Celiklestirme views
            "BulkShipment" => sp.GetService<BulkShipmentAvaloniaViewModel>(),
            "ReturnList" => sp.GetService<ReturnListAvaloniaViewModel>(),
            "ReturnDetail" => sp.GetService<ReturnDetailAvaloniaViewModel>(),
            "LabelPreview" => sp.GetService<LabelPreviewAvaloniaViewModel>(),
            "CargoProviders" => sp.GetService<CargoProvidersAvaloniaViewModel>(),
            // İ-07 Muhasebe & Finans Saglamlastirma views
            "NakitAkis" => sp.GetService<NakitAkisAvaloniaViewModel>(),
            "Komisyon" => sp.GetService<KomisyonAvaloniaViewModel>(),
            "VergiTakvimi" => sp.GetService<VergiTakvimiAvaloniaViewModel>(),
            "SabitGiderler" => sp.GetService<SabitGiderlerAvaloniaViewModel>(),
            "KarlilikAnalizi" => sp.GetService<KarlilikAnaliziAvaloniaViewModel>(),
            "KdvRapor" => sp.GetService<KdvRaporAvaloniaViewModel>(),
            "Bordro" => sp.GetService<BordroAvaloniaViewModel>(),
            "Budget" => sp.GetService<BudgetAvaloniaViewModel>(),
            // İ-11 Görev 4: System Management views
            "NotificationSettings" => sp.GetService<NotificationSettingsAvaloniaViewModel>(),
            "ReportDashboard" => sp.GetService<ReportDashboardAvaloniaViewModel>(),
            "AuditLog" => sp.GetService<AuditLogAvaloniaViewModel>(),
            "Backup" => sp.GetService<BackupAvaloniaViewModel>(),
            // FIX-18 Gorev #13: Buybox Analizi
            "Buybox" => sp.GetService<BuyboxAvaloniaViewModel>(),
            // Missing Fulfillment, ERP, Accounting nav views
            "FulfillmentDashboard" => sp.GetService<FulfillmentDashboardViewModel>(),
            "FulfillmentInbound" => sp.GetService<FulfillmentInboundViewModel>(),
            "FulfillmentInventory" => sp.GetService<FulfillmentInventoryViewModel>(),
            "FulfillmentSettings" => sp.GetService<FulfillmentSettingsViewModel>(),
            "ErpDashboard" => sp.GetService<ErpVm.ErpDashboardViewModel>(),
            "ErpAccountMapping" => sp.GetService<ErpVm.ErpAccountMappingViewModel>(),
            "IncomeExpenseDashboard" => sp.GetService<AcctVm.IncomeExpenseDashboardViewModel>(),
            "IncomeExpenseList" => sp.GetService<AcctVm.IncomeExpenseListViewModel>(),
            "ProfitabilityReport" => sp.GetService<AcctVm.ProfitabilityReportViewModel>(),
            "CashFlowReport" => sp.GetService<CashFlowReportViewModel>(),
            "SalesAnalytics" => sp.GetService<SalesAnalyticsViewModel>(),
            "StockValueReport" => sp.GetService<StockValueReportViewModel>(),
            // V4 — Muhasebe + İzleme + Kanban
            "JournalEntries" => sp.GetService<AcctVm.JournalEntryListViewModel>(),
            "TrialBalance" => sp.GetService<AcctVm.TrialBalanceViewModel>(),
            "CommissionRates" => sp.GetService<AcctVm.CommissionRatesViewModel>(),
            "StaleOrders" => sp.GetService<MonVm.StaleOrdersAvaloniaViewModel>(),
            "OrderKanban" => sp.GetService<OrdVm.OrderKanbanViewModel>(),
            // WPF010 — LogViewer, WPF011 — Export
            "LogViewer" => sp.GetService<LogViewerAvaloniaViewModel>(),
            "Export" => sp.GetService<ExportAvaloniaViewModel>(),
            // WPF005 — BarcodeReader
            "BarcodeReader" => sp.GetService<BarcodeReaderViewModel>(),
            // G042 — 10 missing factory keys
            "BulkProduct" => sp.GetService<BulkProductAvaloniaViewModel>(),
            "Cargo" => sp.GetService<CargoAvaloniaViewModel>(),
            "Cari" => sp.GetService<CariAvaloniaViewModel>(),
            "EInvoice" => sp.GetService<EInvoiceAvaloniaViewModel>(),
            "Onboarding" => sp.GetService<OnboardingWizardAvaloniaViewModel>(),
            "PlatformSync" => sp.GetService<PlatformSyncAvaloniaViewModel>(),
            "StockAlert" => sp.GetService<StockAlertAvaloniaViewModel>(),
            "StockUpdate" => sp.GetService<StockUpdateAvaloniaViewModel>(),
            "TransferWizard" => sp.GetService<TransferWizardAvaloniaViewModel>(),
            "WarehouseSummary" => sp.GetService<WarehouseSummaryAvaloniaViewModel>(),
            "StockTimeline" => sp.GetService<StockTimelineAvaloniaViewModel>(),
            "Quotation" => sp.GetService<QuotationAvaloniaViewModel>(),
            "Billing" => sp.GetService<BillingAvaloniaViewModel>(),
            "PlatformSyncHistory" => sp.GetService<PlatformSyncHistoryAvaloniaViewModel>(),
            "PlatformConnectionTest" => sp.GetService<PlatformConnectionTestAvaloniaViewModel>(),
            // Accounting batch — FixedAsset, FixedExpense, Penalty, TaxRecord
            "FixedAsset" => sp.GetService<AcctVm.FixedAssetAvaloniaViewModel>(),
            "FixedExpense" => sp.GetService<AcctVm.FixedExpenseAvaloniaViewModel>(),
            "Penalty" => sp.GetService<AcctVm.PenaltyAvaloniaViewModel>(),
            "TaxRecord" => sp.GetService<AcctVm.TaxRecordAvaloniaViewModel>(),
            _ => null
        };
    }

    public void Dispose()
    {
        _currentScope?.Dispose();
        _currentScope = null;
    }
}
