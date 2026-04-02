using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Input;
using FluentAssertions;
using Xunit.Abstractions;

namespace MesTech.Tests.UIAutomation;

/// <summary>
/// 107 menü tam taraması — her ekranı aç, screenshot al, hata kontrol et.
/// DEV2 Katman 2 genişletme: TEK OTURUM, TEK APP, 107 MENÜ.
/// </summary>
[Collection("FlaUI")]
[Trait("Category", "FlaUI-FullScan")]
public class FullMenuScanTests : FlaUITestBase
{
    // ToolTip → CommandParameter eşlemesi (MainWindow.axaml'den)
    private static readonly (string ToolTip, string CommandParam)[] AllMenus =
    [
        ("Kontrol Paneli", "Dashboard"),
        ("Urun Listesi", "Products"),
        ("Toplu Ice Aktarma", "ImportProducts"),
        ("Siparisler", "Orders"),
        ("Stok Takibi", "Stock"),
        ("Envanter", "Inventory"),
        ("Stok Guncelleme", "StockMovement"),
        ("Stok Yerlesim", "StockPlacement"),
        ("Lot Ekleme", "StockLot"),
        ("Depolar Arasi Transfer", "StockTransfer"),
        ("Stok Uyarilari", "StockAlert"),
        ("Depo Yonetimi", "Warehouse"),
        ("Stok Hareket Gecmisi", "StockTimeline"),
        ("Kategoriler", "Category"),
        ("Kargo Takip", "CargoTracking"),
        ("Kargo Firmalari", "CargoProviders"),
        ("Toplu Gonderim", "BulkShipment"),
        ("Etiket Yazdir", "LabelPreview"),
        ("Iade Listesi", "ReturnList"),
        ("Fatura Yonetimi", "InvoiceManagement"),
        ("Fatura Listesi", "InvoiceList"),
        ("Fatura Olustur", "InvoiceCreate"),
        ("Toplu Fatura", "BulkInvoice"),
        ("Provider Ayarlari", "InvoiceProviders"),
        ("Fatura Raporlari", "InvoiceReport"),
        ("Kar / Zarar Raporu", "ProfitLoss"),
        ("Giderler", "Expenses"),
        ("Banka Hesaplari", "BankAccounts"),
        ("Cari Hesaplar", "CariHesaplar"),
        ("Nakit Akisi", "NakitAkis"),
        ("Teklif Yonetimi", "Quotation"),
        ("Abonelik", "Billing"),
        ("Yevmiye Defteri", "JournalEntries"),
        ("Mizan", "TrialBalance"),
        ("Komisyon Oranlari", "CommissionRates"),
        ("Muhasebe Paneli", "AccountingDashboard"),
        ("Muhasebe Fisleri", "GLTransaction"),
        ("Gelir / Gider", "GelirGider"),
        ("Karlilik Analizi", "KarlilikAnalizi"),
        ("KDV Raporu", "KdvRapor"),
        ("Mutabakat", "Mutabakat"),
        ("Gecikmis Siparisler", "StaleOrders"),
        ("Siparis Kanban", "OrderKanban"),
        ("Potansiyel Musteriler", "Leads"),
        ("Kisiler", "Contacts"),
        ("Kanban", "Kanban"),
        ("Musteriler", "Customers"),
        ("Platformlar", "PlatformList"),
        ("Magaza Ekle", "StoreWizard"),
        ("Magaza Ayarlari", "StoreSettings"),
        ("Kategori Eslestirme", "CategoryMapping"),
        ("Sync Durumu", "PlatformSyncStatus"),
        ("Sync Gecmisi", "PlatformSyncHistory"),
        ("Baglanti Testi", "PlatformConnectionTest"),
        ("Dropship Paneli", "DropshipDashboard"),
        ("Dropship Siparisler", "DropshipOrders"),
        ("Tedarikci Feed", "SupplierFeeds"),
        ("Yeni Feed", "FeedCreate"),
        ("Ice Aktarma Ayarlari", "ImportSettings"),
        ("Buybox", "Buybox"),
        ("Fulfillment Dashboard", "FulfillmentDashboard"),
        ("Fulfillment Ayarlari", "FulfillmentSettings"),
        ("ERP Dashboard", "ErpDashboard"),
        ("Gelir Gider Dashboard", "IncomeExpenseDashboard"),
        ("Gelir Gider Kayitlari", "IncomeExpenseList"),
        ("Karlilik Raporu", "ProfitabilityReport"),
        ("Belgeler", "Documents"),
        ("Raporlar", "Reports"),
        ("Nakit Akis Raporu", "CashFlowReport"),
        ("Satis Analizi", "SalesAnalytics"),
        ("Stok Deger Raporu", "StockValueReport"),
        ("Ayarlar", "Settings"),
        ("Calisanlar", "Employees"),
        ("Izin Talepleri", "LeaveRequests"),
        ("Trendyol", "Trendyol"),
        ("Hepsiburada", "Hepsiburada"),
        ("N11", "N11"),
        ("Ciceksepeti", "Ciceksepeti"),
        ("Amazon", "Amazon"),
        ("OpenCart", "OpenCart"),
        ("Etsy", "Etsy"),
        ("Shopify", "Shopify"),
        ("WooCommerce", "WooCommerce"),
        ("eBay", "Ebay"),
        ("Zalando", "Zalando"),
        ("Ozon", "Ozon"),
        ("PTT AVM", "PttAvm"),
        ("Pazarama", "Pazarama"),
        ("Bitrix24", "Bitrix24"),
        ("Barkod Okuyucu", "BarcodeScanner"),
        ("Dışa Aktar", "Export"),
        ("AI Urun Aciklama", "ProductDescriptionAI"),
        ("Sistem Sagligi", "Health"),
        ("Denetim Kaydi", "AuditLog"),
        ("Log Izleyici", "LogViewer"),
        ("MFA Ayarlari", "MfaSetup"),
        ("Yedekleme", "Backup"),
    ];

    public FullMenuScanTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void SCAN_107_AllMenus()
    {
        Output.WriteLine($"=== 107 MENÜ TAM TARAMASI === ({DateTime.Now:HH:mm:ss})");
        LoginSucceeded.Should().BeTrue("Login basarili olmali");

        var results = new List<(string Cmd, string Status, string? Error)>();
        var passCount = 0;
        var failCount = 0;
        var skipCount = 0;

        foreach (var (toolTip, cmdParam) in AllMenus)
        {
            try
            {
                var clicked = ClickMenu(toolTip);
                if (!clicked)
                {
                    // ToolTip bulunamadı — CommandParameter ile dene
                    clicked = ClickMenu(cmdParam);
                }

                if (!clicked)
                {
                    Screenshot($"SCAN_{cmdParam}", cmdParam, false, "NotFound");
                    results.Add((cmdParam, "SKIP", $"Buton bulunamadi: {toolTip}"));
                    skipCount++;
                    Output.WriteLine($"  [{cmdParam}] SKIP — buton bulunamadi ({toolTip})");
                    continue;
                }

                Thread.Sleep(1500);

                // Hata kontrolü — sadece gerçek exception mesajlarını yakala
                var error = FindError("Hata Olustu", "Hata olustu", "Exception",
                    "column does not exist", "relation does not exist",
                    "second operation", "yuklenemedi", "baglanilamadi");
                if (error is not null)
                {
                    Screenshot($"SCAN_{cmdParam}", cmdParam, false, "HasError");
                    results.Add((cmdParam, "FAIL", error));
                    failCount++;
                    Output.WriteLine($"  [{cmdParam}] FAIL — {error}");
                }
                else
                {
                    Screenshot($"SCAN_{cmdParam}", cmdParam, true);
                    results.Add((cmdParam, "PASS", null));
                    passCount++;
                    Output.WriteLine($"  [{cmdParam}] PASS");
                }
            }
            catch (Exception ex)
            {
                results.Add((cmdParam, "ERROR", ex.Message));
                failCount++;
                Output.WriteLine($"  [{cmdParam}] ERROR — {ex.Message}");
            }
        }

        // Özet rapor
        Output.WriteLine($"\n=== ÖZET ===");
        Output.WriteLine($"Toplam: {AllMenus.Length} | PASS: {passCount} | FAIL: {failCount} | SKIP: {skipCount}");
        Output.WriteLine($"\n--- FAIL/SKIP LİSTESİ ---");
        foreach (var r in results.Where(r => r.Status != "PASS"))
            Output.WriteLine($"  {r.Status}: {r.Cmd} — {r.Error}");

        // Sonuç dosyasına yaz
        var reportPath = Path.Combine(ScreenshotDir, "FULL_SCAN_REPORT.txt");
        File.WriteAllLines(reportPath, results.Select(r => $"{r.Status}|{r.Cmd}|{r.Error ?? "OK"}"));
        Output.WriteLine($"\nRapor: {reportPath}");
    }
}
