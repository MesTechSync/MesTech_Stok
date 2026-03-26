using CommunityToolkit.Mvvm.ComponentModel;
using MesTech.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using AcctVm = MesTech.Avalonia.ViewModels.Accounting;
using ErpVm = MesTech.Avalonia.ViewModels.Erp;
using MonVm = MesTech.Avalonia.ViewModels.Monitoring;
using OrdVm = MesTech.Avalonia.ViewModels.Orders;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Concrete ViewModel factory backed by IServiceProvider.
/// All ViewModel types are resolved through constructor injection —
/// no static ServiceLocator access. This class is the ONLY place
/// that holds a reference to IServiceProvider.
/// </summary>
public sealed class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _provider;

    public ViewModelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public ObservableObject? Create(string viewName)
    {
        return viewName switch
        {
            // Core views (Dalga 10)
            "Dashboard" => _provider.GetService<DashboardAvaloniaViewModel>(),
            "Leads" => _provider.GetService<LeadsAvaloniaViewModel>(),
            "Kanban" => _provider.GetService<KanbanAvaloniaViewModel>(),
            "ProfitLoss" => _provider.GetService<MesTechStok.Desktop.ViewModels.Finance.ProfitLossViewModel>(),
            "Products" => _provider.GetService<ProductsAvaloniaViewModel>(),
            "Stock" => _provider.GetService<StockAvaloniaViewModel>(),
            "Orders" => _provider.GetService<OrdersAvaloniaViewModel>(),
            "Settings" => _provider.GetService<SettingsAvaloniaViewModel>(),
            "Login" => _provider.GetService<LoginAvaloniaViewModel>(),
            "Category" => _provider.GetService<CategoryAvaloniaViewModel>(),
            // Dalga 11 batch expansion
            "Contacts" => _provider.GetService<ContactsAvaloniaViewModel>(),
            "Employees" => _provider.GetService<EmployeesAvaloniaViewModel>(),
            "LeaveRequests" => _provider.GetService<LeaveRequestsAvaloniaViewModel>(),
            "Documents" => _provider.GetService<DocumentsAvaloniaViewModel>(),
            "Reports" => _provider.GetService<ReportsAvaloniaViewModel>(),
            "Marketplaces" => _provider.GetService<MarketplacesAvaloniaViewModel>(),
            "Expenses" => _provider.GetService<ExpensesAvaloniaViewModel>(),
            "BankAccounts" => _provider.GetService<BankAccountsAvaloniaViewModel>(),
            // Dalga 14+15 functional views
            "Inventory" => _provider.GetService<InventoryAvaloniaViewModel>(),
            "InvoiceManagement" => _provider.GetService<InvoiceManagementAvaloniaViewModel>(),
            "Customers" => _provider.GetService<CustomerAvaloniaViewModel>(),
            "CariHesaplar" => _provider.GetService<CariHesaplarAvaloniaViewModel>(),
            "SyncStatus" => _provider.GetService<SyncStatusAvaloniaViewModel>(),
            "StockMovement" => _provider.GetService<StockMovementAvaloniaViewModel>(),
            "CargoTracking" => _provider.GetService<CargoTrackingAvaloniaViewModel>(),
            // Dalga 17 batch (T007)
            "About" => _provider.GetService<AboutAvaloniaViewModel>(),
            "AccountingDashboard" => _provider.GetService<AccountingDashboardAvaloniaViewModel>(),
            "Activity" => _provider.GetService<ActivityAvaloniaViewModel>(),
            "Amazon" => _provider.GetService<AmazonAvaloniaViewModel>(),
            "AmazonEu" => _provider.GetService<AmazonEuAvaloniaViewModel>(),
            "Barcode" => _provider.GetService<BarcodeAvaloniaViewModel>(),
            "Calendar" => _provider.GetService<CalendarAvaloniaViewModel>(),
            "CargoSettings" => _provider.GetService<CargoSettingsAvaloniaViewModel>(),
            "Ciceksepeti" => _provider.GetService<CiceksepetiAvaloniaViewModel>(),
            "Contact" => _provider.GetService<ContactAvaloniaViewModel>(),
            "CrmDashboard" => _provider.GetService<CrmDashboardAvaloniaViewModel>(),
            "PlatformMessages" => _provider.GetService<PlatformMessagesAvaloniaViewModel>(),
            "CrmSettings" => _provider.GetService<CrmSettingsAvaloniaViewModel>(),
            "Deals" => _provider.GetService<DealsAvaloniaViewModel>(),
            "Department" => _provider.GetService<DepartmentAvaloniaViewModel>(),
            "DocumentFolder" => _provider.GetService<DocumentFolderAvaloniaViewModel>(),
            "DocumentManager" => _provider.GetService<DocumentManagerAvaloniaViewModel>(),
            "Ebay" => _provider.GetService<EbayAvaloniaViewModel>(),
            "ErpSettings" => _provider.GetService<ErpSettingsAvaloniaViewModel>(),
            "GLTransaction" => _provider.GetService<GLTransactionAvaloniaViewModel>(),
            "GelirGider" => _provider.GetService<GelirGiderAvaloniaViewModel>(),
            "Health" => _provider.GetService<HealthAvaloniaViewModel>(),
            "Hepsiburada" => _provider.GetService<HepsiburadaAvaloniaViewModel>(),
            "InvoiceSettings" => _provider.GetService<InvoiceSettingsAvaloniaViewModel>(),
            "KanbanBoard" => _provider.GetService<KanbanBoardAvaloniaViewModel>(),
            "KarZarar" => _provider.GetService<KarZararAvaloniaViewModel>(),
            "Mesa" => _provider.GetService<MesaAvaloniaViewModel>(),
            "MultiTenant" => _provider.GetService<MultiTenantAvaloniaViewModel>(),
            "Mutabakat" => _provider.GetService<MutabakatAvaloniaViewModel>(),
            "N11" => _provider.GetService<N11AvaloniaViewModel>(),
            "Notification" => _provider.GetService<NotificationAvaloniaViewModel>(),
            "OpenCart" => _provider.GetService<OpenCartAvaloniaViewModel>(),
            "OrderDetail" => _provider.GetService<OrderDetailAvaloniaViewModel>(),
            "OrderList" => _provider.GetService<OrderListAvaloniaViewModel>(),
            "Ozon" => _provider.GetService<OzonAvaloniaViewModel>(),
            "Pazarama" => _provider.GetService<PazaramaAvaloniaViewModel>(),
            "Pipeline" => _provider.GetService<PipelineAvaloniaViewModel>(),
            "Projects" => _provider.GetService<ProjectsAvaloniaViewModel>(),
            "PttAvm" => _provider.GetService<PttAvmAvaloniaViewModel>(),
            "Report" => _provider.GetService<ReportAvaloniaViewModel>(),
            "Shipment" => _provider.GetService<ShipmentAvaloniaViewModel>(),
            "StoreManagement" => _provider.GetService<StoreManagementAvaloniaViewModel>(),
            "Supplier" => _provider.GetService<SupplierAvaloniaViewModel>(),
            "Tenant" => _provider.GetService<TenantAvaloniaViewModel>(),
            "TimeEntry" => _provider.GetService<TimeEntryAvaloniaViewModel>(),
            "Trendyol" => _provider.GetService<TrendyolAvaloniaViewModel>(),
            "UserManagement" => _provider.GetService<UserManagementAvaloniaViewModel>(),
            "Warehouse" => _provider.GetService<WarehouseAvaloniaViewModel>(),
            // EMR-06 Gorev 4B: Stok Yerlesim + Lot + Transfer
            "StockPlacement" => _provider.GetService<StockPlacementAvaloniaViewModel>(),
            "StockLot" => _provider.GetService<StockLotAvaloniaViewModel>(),
            "StockTransfer" => _provider.GetService<StockTransferAvaloniaViewModel>(),
            // Invoice views (e-Fatura batch)
            "InvoiceList" => _provider.GetService<InvoiceListAvaloniaViewModel>(),
            "InvoiceCreate" => _provider.GetService<InvoiceCreateAvaloniaViewModel>(),
            "BulkInvoice" => _provider.GetService<BulkInvoiceAvaloniaViewModel>(),
            "InvoicePdf" => _provider.GetService<InvoicePdfAvaloniaViewModel>(),
            "InvoiceProviders" => _provider.GetService<InvoiceProviderSettingsAvaloniaViewModel>(),
            "InvoiceReport" => _provider.GetService<InvoiceReportAvaloniaViewModel>(),
            "Welcome" => _provider.GetService<WelcomeAvaloniaViewModel>(),
            "WorkSchedule" => _provider.GetService<WorkScheduleAvaloniaViewModel>(),
            "WorkTask" => _provider.GetService<WorkTaskAvaloniaViewModel>(),
            // EMR-10 Platform + Dropshipping views
            "PlatformList" => _provider.GetService<PlatformListAvaloniaViewModel>(),
            "StoreWizard" => _provider.GetService<StoreWizardAvaloniaViewModel>(),
            "CategoryMapping" => _provider.GetService<CategoryMappingAvaloniaViewModel>(),
            "DropshipDashboard" => _provider.GetService<DropshipDashboardAvaloniaViewModel>(),
            "FeedPreview" => _provider.GetService<FeedPreviewAvaloniaViewModel>(),
            "StoreSettings" => _provider.GetService<StoreSettingsAvaloniaViewModel>(),
            "StoreDetail" => _provider.GetService<StoreDetailAvaloniaViewModel>(),
            "ProductFetch" => _provider.GetService<ProductFetchAvaloniaViewModel>(),
            "SupplierFeeds" => _provider.GetService<SupplierFeedListAvaloniaViewModel>(),
            "FeedCreate" => _provider.GetService<FeedCreateAvaloniaViewModel>(),
            "DropshipOrders" => _provider.GetService<DropshipOrdersAvaloniaViewModel>(),
            "DropshipProfit" => _provider.GetService<DropshipProfitAvaloniaViewModel>(),
            "ImportSettings" => _provider.GetService<ImportSettingsAvaloniaViewModel>(),
            "ImportProducts" => _provider.GetService<ImportProductsAvaloniaViewModel>(),
            "ProductVariantMatrix" => _provider.GetService<ProductVariantMatrixViewModel>(),
            "PlatformSyncStatus" => _provider.GetService<PlatformSyncStatusAvaloniaViewModel>(),
            // İ-05 Siparis/Kargo Celiklestirme views
            "BulkShipment" => _provider.GetService<BulkShipmentAvaloniaViewModel>(),
            "ReturnList" => _provider.GetService<ReturnListAvaloniaViewModel>(),
            "ReturnDetail" => _provider.GetService<ReturnDetailAvaloniaViewModel>(),
            "LabelPreview" => _provider.GetService<LabelPreviewAvaloniaViewModel>(),
            "CargoProviders" => _provider.GetService<CargoProvidersAvaloniaViewModel>(),
            // İ-07 Muhasebe & Finans Saglamlastirma views
            "NakitAkis" => _provider.GetService<NakitAkisAvaloniaViewModel>(),
            "Komisyon" => _provider.GetService<KomisyonAvaloniaViewModel>(),
            "VergiTakvimi" => _provider.GetService<VergiTakvimiAvaloniaViewModel>(),
            "SabitGiderler" => _provider.GetService<SabitGiderlerAvaloniaViewModel>(),
            "KarlilikAnalizi" => _provider.GetService<KarlilikAnaliziAvaloniaViewModel>(),
            "KdvRapor" => _provider.GetService<KdvRaporAvaloniaViewModel>(),
            "Bordro" => _provider.GetService<BordroAvaloniaViewModel>(),
            "Budget" => _provider.GetService<BudgetAvaloniaViewModel>(),
            // İ-11 Görev 4: System Management views
            "NotificationSettings" => _provider.GetService<NotificationSettingsAvaloniaViewModel>(),
            "ReportDashboard" => _provider.GetService<ReportDashboardAvaloniaViewModel>(),
            "AuditLog" => _provider.GetService<AuditLogAvaloniaViewModel>(),
            "Backup" => _provider.GetService<BackupAvaloniaViewModel>(),
            // FIX-18 Gorev #13: Buybox Analizi
            "Buybox" => _provider.GetService<BuyboxAvaloniaViewModel>(),
            // Missing Fulfillment, ERP, Accounting nav views
            "FulfillmentDashboard" => _provider.GetService<FulfillmentDashboardViewModel>(),
            "FulfillmentInbound" => _provider.GetService<FulfillmentInboundViewModel>(),
            "FulfillmentInventory" => _provider.GetService<FulfillmentInventoryViewModel>(),
            "FulfillmentSettings" => _provider.GetService<FulfillmentSettingsViewModel>(),
            "ErpDashboard" => _provider.GetService<ErpVm.ErpDashboardViewModel>(),
            "ErpAccountMapping" => _provider.GetService<ErpVm.ErpAccountMappingViewModel>(),
            "IncomeExpenseDashboard" => _provider.GetService<AcctVm.IncomeExpenseDashboardViewModel>(),
            "IncomeExpenseList" => _provider.GetService<AcctVm.IncomeExpenseListViewModel>(),
            "ProfitabilityReport" => _provider.GetService<AcctVm.ProfitabilityReportViewModel>(),
            "CashFlowReport" => _provider.GetService<CashFlowReportViewModel>(),
            "SalesAnalytics" => _provider.GetService<SalesAnalyticsViewModel>(),
            "StockValueReport" => _provider.GetService<StockValueReportViewModel>(),
            // V4 — Muhasebe + İzleme + Kanban
            "JournalEntries" => _provider.GetService<AcctVm.JournalEntryListViewModel>(),
            "TrialBalance" => _provider.GetService<AcctVm.TrialBalanceViewModel>(),
            "CommissionRates" => _provider.GetService<AcctVm.CommissionRatesViewModel>(),
            "StaleOrders" => _provider.GetService<MonVm.StaleOrdersAvaloniaViewModel>(),
            "OrderKanban" => _provider.GetService<OrdVm.OrderKanbanViewModel>(),
            // G042 — 10 missing factory keys
            "BulkProduct" => _provider.GetService<BulkProductAvaloniaViewModel>(),
            "Cargo" => _provider.GetService<CargoAvaloniaViewModel>(),
            "Cari" => _provider.GetService<CariAvaloniaViewModel>(),
            "EInvoice" => _provider.GetService<EInvoiceAvaloniaViewModel>(),
            "Onboarding" => _provider.GetService<OnboardingWizardAvaloniaViewModel>(),
            "PlatformSync" => _provider.GetService<PlatformSyncAvaloniaViewModel>(),
            "StockAlert" => _provider.GetService<StockAlertAvaloniaViewModel>(),
            "StockUpdate" => _provider.GetService<StockUpdateAvaloniaViewModel>(),
            "TransferWizard" => _provider.GetService<TransferWizardAvaloniaViewModel>(),
            "WarehouseSummary" => _provider.GetService<WarehouseSummaryAvaloniaViewModel>(),
            _ => null
        };
    }
}
