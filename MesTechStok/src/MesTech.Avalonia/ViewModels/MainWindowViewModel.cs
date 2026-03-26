using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Avalonia.Services;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// MainWindow ViewModel — sidebar navigation between views.
/// Uses IViewModelFactory (proper DI) instead of raw IServiceProvider (ServiceLocator).
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableObject? currentView;

    [ObservableProperty]
    private string currentViewTitle = "Dashboard";

    private readonly IViewModelFactory _viewModelFactory;

    public MainWindowViewModel(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    [RelayCommand]
    private async Task NavigateTo(string viewName)
    {
        var resolved = _viewModelFactory.Create(viewName);
        if (resolved is not null)
        {
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
            _ => CurrentViewTitle
        };
    }
}
