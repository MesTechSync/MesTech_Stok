using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Avalonia.Services;
using MediatR;

// ReSharper disable once RedundantUsingDirective — SubscriptionTier needed for TierChanged event signature
namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// MainWindow ViewModel — sidebar navigation between views.
/// Uses IViewModelFactory (proper DI) instead of raw IServiceProvider (ServiceLocator).
/// FeatureGateService controls sidebar item visibility per subscription tier.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, INavigationService
{
    [ObservableProperty]
    private ObservableObject? currentView;

    [ObservableProperty]
    private string currentViewTitle = "Dashboard";

    [ObservableProperty]
    private string selectedMenuItem = "AppHub";

    [ObservableProperty]
    private string breadcrumb = "Ana Sayfa";

    private readonly IViewModelFactory _viewModelFactory;
    private readonly IFeatureGateService _featureGate;

    // ── Sidebar visibility helpers (bound by MainWindow sidebar ItemsControl) ──
    public bool ShowReports        => _featureGate.IsEnabled("Reports");
    public bool ShowAnalytics      => _featureGate.IsEnabled("Analytics");
    public bool ShowCrm            => _featureGate.IsEnabled("CRM");
    public bool ShowCargo          => _featureGate.IsEnabled("Cargo");
    public bool ShowInvoice        => _featureGate.IsEnabled("Invoice");
    public bool ShowExport         => _featureGate.IsEnabled("Export");
    public bool ShowMultiPlatform  => _featureGate.IsEnabled("MultiPlatform");
    public bool ShowAiInsight      => _featureGate.IsEnabled("AIInsight");
    public bool ShowMesaBridge     => _featureGate.IsEnabled("MesaBridge");
    public bool ShowAutomation     => _featureGate.IsEnabled("Automation");
    public bool ShowWebhook        => _featureGate.IsEnabled("Webhook");
    public bool ShowApiAccess      => _featureGate.IsEnabled("ApiAccess");
    public bool ShowLogViewer      => _featureGate.IsEnabled("LogViewer");
    public bool ShowHealthMonitor  => _featureGate.IsEnabled("HealthMonitor");

    public MainWindowViewModel(IViewModelFactory viewModelFactory, IFeatureGateService featureGate)
    {
        _viewModelFactory = viewModelFactory;
        _featureGate      = featureGate;

        // Re-evaluate sidebar visibility when tier changes (named handler — unsubscribe possible [EL-02])
        _featureGate.TierChanged += OnTierChanged;
    }

    private void OnTierChanged(object? sender, SubscriptionTier e) => RefreshSidebarVisibility();

    private void RefreshSidebarVisibility()
    {
        OnPropertyChanged(nameof(ShowReports));
        OnPropertyChanged(nameof(ShowAnalytics));
        OnPropertyChanged(nameof(ShowCrm));
        OnPropertyChanged(nameof(ShowCargo));
        OnPropertyChanged(nameof(ShowInvoice));
        OnPropertyChanged(nameof(ShowExport));
        OnPropertyChanged(nameof(ShowMultiPlatform));
        OnPropertyChanged(nameof(ShowAiInsight));
        OnPropertyChanged(nameof(ShowMesaBridge));
        OnPropertyChanged(nameof(ShowAutomation));
        OnPropertyChanged(nameof(ShowWebhook));
        OnPropertyChanged(nameof(ShowApiAccess));
        OnPropertyChanged(nameof(ShowLogViewer));
        OnPropertyChanged(nameof(ShowHealthMonitor));
    }

    [RelayCommand]
    private Task NavigateTo(string viewName)
    {
        var resolved = _viewModelFactory.Create(viewName);
        if (resolved is not null)
        {
            (CurrentView as IDisposable)?.Dispose();
            CurrentView = resolved;
            SelectedMenuItem = viewName;
            // BaseView.OnAttachedToVisualTree → InitializeAsync → LoadAsync otomatik çağrılır.
            // Burada tekrar çağırmak çift yükleme (2x DB sorgusu) yapıyordu — kaldırıldı.
        }

        CurrentViewTitle = viewName switch
        {
            // Core views (Dalga 10)
            "Dashboard" => "Kontrol Paneli",
            "Leads" => "Potansiyel Musteriler",
            "Kanban" => "Firsatlar — Kanban",
            "ProfitLoss" => "Kar / Zarar Raporu",
            "Products" => "Urunler",
            "Stock" => "Stok Yonetimi",
            "Orders" => "Siparis Yonetimi",
            "Settings" => "Ayarlar",
            "Login" => "Giris",
            "Category" => "Kategoriler",
            // Dalga 11 batch expansion
            "Contacts" => "CRM Kisiler",
            "Employees" => "Calisanlar",
            "LeaveRequests" => "Izin Talepleri",
            "Documents" => "Belge Yonetimi",
            "Reports" => "Raporlar",
            "Marketplaces" => "Pazaryerleri",
            "Expenses" => "Giderler",
            "BankAccounts" => "Banka Hesaplari",
            // Dalga 14+15 functional views
            "Inventory" => "Envanter",
            "InvoiceManagement" => "e-Fatura Yonetimi",
            "Customers" => "Musteriler",
            "CariHesaplar" => "Cari Hesaplar",
            "SyncStatus" => "Platform Senkronizasyon",
            "StockMovement" => "Stok Guncelleme",
            "CargoTracking" => "Kargo Takip",
            // EMR-06 Gorev 4C: Stok Yerlesim + Lot + Transfer
            "StockPlacement" => "Stok Yerlesim",
            "StockLot" => "Lot Ekleme",
            "StockTransfer" => "Depolar Arasi Transfer",
            // Dalga 17 batch (T007) — EMR-12 navigation titles
            "About" => "Hakkinda",
            "AccountingDashboard" => "Muhasebe Paneli",
            "Activity" => "Aktiviteler",
            "Amazon" => "Amazon",
            "AmazonEu" => "Amazon EU",
            "Barcode" => "Barkod Islemleri",
            "Calendar" => "Takvim",
            "CargoSettings" => "Kargo Ayarlari",
            "Ciceksepeti" => "Ciceksepeti",
            "Contact" => "Iletisim",
            "CrmDashboard" => "CRM Paneli",
            "PlatformMessages" => "Platform Mesajlari",
            "CrmSettings" => "CRM Ayarlari",
            "Deals" => "Firsatlar",
            "Department" => "Departmanlar",
            "DocumentFolder" => "Belge Klasorleri",
            "DocumentManager" => "Belge Yoneticisi",
            "Ebay" => "eBay",
            "ErpSettings" => "ERP Ayarlari",
            "GLTransaction" => "Muhasebe Fisleri",
            "GelirGider" => "Gelir / Gider",
            "Health" => "Sistem Sagligi",
            "Hepsiburada" => "Hepsiburada",
            "InvoiceSettings" => "Fatura Ayarlari",
            "KanbanBoard" => "Kanban Panosu",
            "KarZarar" => "Kar / Zarar",
            "Mesa" => "MESA",
            "MultiTenant" => "Multi-Tenant",
            "Mutabakat" => "Mutabakat",
            "N11" => "N11",
            "Notification" => "Bildirimler",
            "OpenCart" => "OpenCart",
            "OrderDetail" => "Siparis Detay",
            "OrderList" => "Siparis Listesi",
            "Ozon" => "Ozon",
            "Etsy" => "Etsy",
            "Shopify" => "Shopify",
            "WooCommerce" => "WooCommerce",
            "Zalando" => "Zalando",
            "Pazarama" => "Pazarama",
            "Pipeline" => "Pipeline",
            "Projects" => "Projeler",
            "PttAvm" => "PTT AVM",
            "Report" => "Rapor",
            "Shipment" => "Kargo Takip",
            "StoreManagement" => "Magaza Yonetimi",
            "Supplier" => "Tedarikciler",
            "Tenant" => "Tenant Yonetimi",
            "TimeEntry" => "Zaman Kaydi",
            "Trendyol" => "Trendyol",
            "UserManagement" => "Kullanici Yonetimi",
            "Warehouse" => "Depo Yonetimi",
            // Invoice views (e-Fatura batch)
            "InvoiceList" => "Fatura Listesi",
            "InvoiceCreate" => "Fatura Olustur",
            "BulkInvoice" => "Toplu Fatura",
            "InvoicePdf" => "Fatura PDF",
            "InvoiceProviders" => "Provider Ayarlari",
            "InvoiceReport" => "Fatura Raporlari",
            "Welcome" => "Hos Geldiniz",
            "WorkSchedule" => "Calisma Takvimi",
            "WorkTask" => "Gorevler",
            // Missing title entries
            "Komisyon" => "Komisyon Oranlari",
            "VergiTakvimi" => "Vergi Takvimi",
            "SabitGiderler" => "Sabit Giderler",
            "KarlilikAnalizi" => "Karlilik Analizi",
            "KdvRapor" => "KDV Raporu",
            "Bordro" => "Bordro Yonetimi",
            "Budget" => "Butce Planlama",
            "NotificationSettings" => "Bildirim Ayarlari",
            "ReportDashboard" => "Rapor Merkezi",
            "AuditLog" => "Denetim Kayitlari",
            "ProductDescriptionAI" => "AI Ürün Açıklama",
            "Backup" => "Yedekleme",
            "Buybox" => "Buybox Analizi",
            "ImportProducts" => "Urun Iceaktar",
            "ProductVariantMatrix" => "Varyant Matrisi",
            "FeedPreview" => "Feed Onizleme",
            "StoreDetail" => "Magaza Detay",
            "ProductFetch" => "Urun Cek",
            "ReturnDetail" => "Iade Detay",
            // V4 — Muhasebe + İzleme + Kanban
            "JournalEntries" => "Yevmiye Defteri",
            "TrialBalance" => "Mizan",
            "CommissionRates" => "Komisyon Oranlari",
            "StaleOrders" => "Gecikmis Siparisler",
            "OrderKanban" => "Siparis Kanban",
            // WPF010 — LogViewer, WPF011 — Export
            "LogViewer" => "Uygulama Logları",
            "Export" => "Dışa Aktar",
            // WPF005 — BarcodeReader
            "BarcodeReader" => "Barkod Okuyucu — Sayım/Giriş/Çıkış",
            // G042 — 10 missing title entries
            "BulkProduct" => "Toplu Urun Islemleri",
            "Cargo" => "Kargo",
            "Cari" => "Cari",
            "EInvoice" => "e-Fatura",
            "Onboarding" => "Baslangic Sihirbazi",
            "PlatformSync" => "Platform Senkronizasyon",
            "StockAlert" => "Stok Uyarilari",
            "StockUpdate" => "Stok Guncelleme",
            "TransferWizard" => "Transfer Sihirbazi",
            "WarehouseSummary" => "Depo Ozeti",
            // Accounting batch
            "FixedAsset" => "Duran Varliklar",
            "FixedExpense" => "Sabit Giderler",
            "Penalty" => "Ceza / Kesinti",
            "TaxRecord" => "Vergi Kayitlari",
            // Missing title mappings (DEV2 Tur 3 keşif — 36 factory key)
            "AppHub" => "Ana Ekran",
            "BarcodeScanner" => "Barkod Tarayici",
            "Billing" => "Abonelik & Fatura",
            "BulkShipment" => "Toplu Kargo",
            "Campaign" => "Kampanya Yonetimi",
            "CargoProviders" => "Kargo Firmalari",
            "CashFlowReport" => "Nakit Akis Raporu",
            "DropshipDashboard" => "Dropshipping Paneli",
            "DropshipOrders" => "Dropship Siparisler",
            "DropshipProfit" => "Dropship Karlilik",
            "DropshippingPool" => "Dropship Havuz",
            "Cheque" => "Cek/Senet Takip",
            "ErpAccountMapping" => "ERP Hesap Esleme",
            "ErpDashboard" => "ERP Paneli",
            "FeedCreate" => "Feed Olustur",
            "FulfillmentDashboard" => "Fulfillment Paneli",
            "FulfillmentInbound" => "Fulfillment Giris",
            "FulfillmentInventory" => "Fulfillment Envanter",
            "FulfillmentSettings" => "Fulfillment Ayarlari",
            "IncomeExpenseDashboard" => "Gelir Gider Paneli",
            "IncomeExpenseList" => "Gelir Gider Listesi",
            "LabelPreview" => "Etiket Onizleme",
            "MfaSetup" => "MFA Kurulumu",
            "NakitAkis" => "Nakit Akis",
            "PlatformConnectionTest" => "Platform Baglanti Testi",
            "PlatformList" => "Platform Listesi",
            "PlatformSyncHistory" => "Senkronizasyon Gecmisi",
            "PlatformSyncStatus" => "Senkronizasyon Durumu",
            "ProfitabilityReport" => "Karlilik Raporu",
            "Quotation" => "Teklif Yonetimi",
            "ReturnList" => "Iade Listesi",
            "SalesAnalytics" => "Satis Analitikleri",
            "Settlement" => "Hesap Kesim",
            "StockTimeline" => "Stok Zaman Cigisi",
            "StockValueReport" => "Stok Deger Raporu",
            "StoreSettings" => "Magaza Ayarlari",
            "StoreWizard" => "Magaza Sihirbazi",
            "SupplierFeeds" => "Tedarikci Feed'leri",
            // D7-035: Missing sidebar nav titles
            "Bitrix24" => "Bitrix24 CRM",
            "CategoryMapping" => "Kategori Eslestirme",
            "ImportSettings" => "Iceri Aktarma Ayarlari",
            _ => CurrentViewTitle
        };

        // DEV2-03: Breadcrumb güncelle
        var category = viewName switch
        {
            "AppHub" or "Dashboard" or "Welcome" or "Onboarding" => "Ana Sayfa",
            "Products" or "ImportProducts" or "BulkProduct" or "ProductVariantMatrix" or "ProductFetch" or "ProductDescriptionAI" or "Barcode" or "BarcodeScanner" or "BarcodeReader" or "Buybox" => "Urunler",
            "Orders" or "OrderList" or "OrderDetail" or "OrderKanban" or "StaleOrders" => "Siparisler",
            "Stock" or "Inventory" or "StockMovement" or "StockPlacement" or "StockLot" or "StockTransfer" or "StockAlert" or "StockUpdate" or "StockTimeline" or "StockValueReport" or "Warehouse" or "WarehouseSummary" or "TransferWizard" => "Stok",
            "Category" or "CategoryMapping" => "Kategoriler",
            "CargoTracking" or "CargoProviders" or "BulkShipment" or "LabelPreview" or "Shipment" or "Cargo" => "Kargo",
            "ReturnList" or "ReturnDetail" => "Iadeler",
            "InvoiceManagement" or "InvoiceList" or "InvoiceCreate" or "BulkInvoice" or "InvoiceProviders" or "InvoiceReport" or "InvoicePdf" or "EInvoice" => "E-Fatura",
            "ProfitLoss" or "Expenses" or "BankAccounts" or "CariHesaplar" or "Cari" or "NakitAkis" or "CashFlowReport" or "Quotation" or "Billing" or "Budget" or "Settlement" or "SalesAnalytics" or "ProfitabilityReport" or "Cheque" => "Finans",
            "JournalEntries" or "TrialBalance" or "CommissionRates" or "AccountingDashboard" or "GLTransaction" or "KarZarar" or "GelirGider" or "KarlilikAnalizi" or "KdvRapor" or "Mutabakat" or "Komisyon" or "VergiTakvimi" or "SabitGiderler" or "Bordro" or "FixedAsset" or "FixedExpense" or "Penalty" or "TaxRecord" or "IncomeExpenseDashboard" or "IncomeExpenseList" => "Muhasebe",
            "Trendyol" or "Hepsiburada" or "N11" or "Ciceksepeti" or "Amazon" or "AmazonEu" or "Ebay" or "Ozon" or "Etsy" or "Shopify" or "WooCommerce" or "Zalando" or "PttAvm" or "Pazarama" or "OpenCart" or "Bitrix24" or "Marketplaces" or "PlatformList" or "PlatformSync" or "PlatformSyncStatus" or "PlatformSyncHistory" or "PlatformConnectionTest" or "PlatformMessages" or "SyncStatus" => "Pazaryerleri",
            "Settings" or "CargoSettings" or "ErpSettings" or "InvoiceSettings" or "CrmSettings" or "NotificationSettings" or "StoreSettings" or "StoreDetail" or "StoreWizard" or "StoreManagement" or "ImportSettings" or "FulfillmentSettings" or "MfaSetup" or "MultiTenant" or "Tenant" or "UserManagement" or "Backup" => "Ayarlar",
            "Contacts" or "Contact" or "Customers" or "Leads" or "Kanban" or "KanbanBoard" or "Deals" or "Pipeline" or "CrmDashboard" or "Campaign" => "CRM",
            "Employees" or "LeaveRequests" or "Department" or "TimeEntry" or "WorkSchedule" or "WorkTask" or "Projects" => "Insan Kaynaklari",
            "Documents" or "DocumentFolder" or "DocumentManager" or "Export" => "Belgeler",
            "FulfillmentDashboard" or "FulfillmentInbound" or "FulfillmentInventory" => "Fulfillment",
            "ErpDashboard" or "ErpAccountMapping" => "ERP",
            "DropshipDashboard" or "DropshipOrders" or "DropshipProfit" or "DropshippingPool" or "FeedPreview" or "FeedCreate" or "SupplierFeeds" or "Supplier" => "Dropshipping",
            "Reports" or "Report" or "ReportDashboard" => "Raporlar",
            "Health" or "LogViewer" or "AuditLog" or "Mesa" or "Notification" => "Sistem",
            "About" or "Login" or "Activity" or "Calendar" => "Genel",
            _ => "Diger"
        };
        Breadcrumb = category == "Ana Sayfa" ? "Ana Sayfa" : $"Ana Sayfa > {category} > {CurrentViewTitle}";

        return Task.CompletedTask;
    }

    // INavigationService implementation — delegates to existing NavigateTo
    Task INavigationService.NavigateToAsync(string viewName) => NavigateTo(viewName);

    async Task INavigationService.NavigateToAsync(string viewName, IDictionary<string, object?> parameters)
    {
        await NavigateTo(viewName);
        if (CurrentView is INavigationAware aware)
            await aware.OnNavigatedToAsync(parameters);
    }

    protected override void OnDispose()
    {
        _featureGate.TierChanged -= OnTierChanged;
    }
}
