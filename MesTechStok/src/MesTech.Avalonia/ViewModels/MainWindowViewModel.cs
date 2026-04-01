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
    private async Task NavigateTo(string viewName)
    {
        var resolved = _viewModelFactory.Create(viewName);
        if (resolved is not null)
        {
            (CurrentView as IDisposable)?.Dispose();
            CurrentView = resolved;
            // G040 FIX: Trigger data loading for the new view
            if (resolved is ViewModelBase vmBase)
            {
                try { await vmBase.LoadAsync(); }
                catch { /* View handles its own error state via HasError */ }
            }
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
            "ProductDescriptionAI" => "AI Urun Aciklama",
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
            _ => CurrentViewTitle
        };
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
