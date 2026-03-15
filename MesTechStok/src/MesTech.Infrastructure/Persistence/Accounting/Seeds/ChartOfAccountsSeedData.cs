using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Infrastructure.Persistence.Accounting.Seeds;

/// <summary>
/// Tekduzen Hesap Plani (THP) seed data.
/// Temel hesap gruplari: 1xx Donen Varliklar, 3xx Kisa Vadeli Yabanci Kaynaklar,
/// 5xx Gelirler, 6xx Giderler, 7xx Maliyet Hesaplari.
/// </summary>
public static class ChartOfAccountsSeedData
{
    /// <summary>
    /// Varsayilan hesap planini olusturur.
    /// </summary>
    public static IReadOnlyList<ChartOfAccounts> GetDefaultAccounts(Guid tenantId)
    {
        var accounts = new List<ChartOfAccounts>();

        // ═══════════════════════════════════════
        // 1xx — DONEN VARLIKLAR (Asset)
        // ═══════════════════════════════════════
        accounts.Add(ChartOfAccounts.Create(tenantId, "100", "Kasa", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "100.01", "TL Kasa", AccountType.Asset, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "101", "Alinan Cekler", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "102", "Bankalar", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "102.01", "Vadesiz TL Hesap", AccountType.Asset, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "102.02", "Vadeli TL Hesap", AccountType.Asset, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "108", "Diger Hazir Degerler", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "120", "Alicilar", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "120.01", "Platform Alacaklari", AccountType.Asset, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "120.01.001", "Trendyol Alacak", AccountType.Asset, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "120.01.002", "Hepsiburada Alacak", AccountType.Asset, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "120.01.003", "N11 Alacak", AccountType.Asset, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "120.01.004", "Ciceksepeti Alacak", AccountType.Asset, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "120.01.005", "Amazon Alacak", AccountType.Asset, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "121", "Alacak Senetleri", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "126", "Verilen Depozito ve Teminatlar", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "150", "Ilk Madde ve Malzeme", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "152", "Mamul Stoklar", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "153", "Ticari Mallar", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "153.01", "Satisa Hazir Urunler", AccountType.Asset, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "159", "Verilen Siparis Avanslari", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "190", "Devreden KDV", AccountType.Asset, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "191", "Indirilecek KDV", AccountType.Asset, level: 1));

        // ═══════════════════════════════════════
        // 3xx — KISA VADELI YABANCI KAYNAKLAR (Liability)
        // ═══════════════════════════════════════
        accounts.Add(ChartOfAccounts.Create(tenantId, "300", "Banka Kredileri", AccountType.Liability, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "320", "Saticilar", AccountType.Liability, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "320.01", "Tedarikci Borclari", AccountType.Liability, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "321", "Borc Senetleri", AccountType.Liability, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "335", "Personele Borclar", AccountType.Liability, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "340", "Alinan Siparis Avanslari", AccountType.Liability, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "360", "Odenecek Vergi ve Fonlar", AccountType.Liability, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "360.01", "Odenecek KDV", AccountType.Liability, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "360.02", "Odenecek Gelir Vergisi Stopaji", AccountType.Liability, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "361", "Odenecek Sosyal Guvenlik Kesintileri", AccountType.Liability, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "391", "Hesaplanan KDV", AccountType.Liability, level: 1));

        // ═══════════════════════════════════════
        // 5xx — GELIRLER (Revenue)
        // ═══════════════════════════════════════
        accounts.Add(ChartOfAccounts.Create(tenantId, "500", "Sermaye", AccountType.Equity, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "570", "Gecmis Yillar Karlari", AccountType.Equity, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "580", "Gecmis Yillar Zararlari", AccountType.Equity, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "590", "Donem Net Kari", AccountType.Equity, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "591", "Donem Net Zarari", AccountType.Equity, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "600", "Yurtici Satislar", AccountType.Revenue, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "600.01", "Platform Satislari", AccountType.Revenue, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "600.01.001", "Trendyol Satislari", AccountType.Revenue, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "600.01.002", "Hepsiburada Satislari", AccountType.Revenue, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "600.01.003", "N11 Satislari", AccountType.Revenue, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "600.01.004", "Ciceksepeti Satislari", AccountType.Revenue, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "600.01.005", "Amazon Satislari", AccountType.Revenue, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "601", "Yurtdisi Satislar", AccountType.Revenue, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "602", "Diger Gelirler", AccountType.Revenue, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "610", "Satis Indirimleri (-)", AccountType.Revenue, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "611", "Satis Iadeleri (-)", AccountType.Revenue, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "612", "Satis Iskontosu (-)", AccountType.Revenue, level: 1));

        // ═══════════════════════════════════════
        // 6xx — GIDERLER (Expense)
        // ═══════════════════════════════════════
        accounts.Add(ChartOfAccounts.Create(tenantId, "620", "Satilan Malin Maliyeti (-)", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "621", "Satilan Ticari Malin Maliyeti (-)", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "630", "Arastirma ve Gelistirme Giderleri (-)", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "631", "Pazarlama Satis Dagitim Giderleri (-)", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "631.01", "Platform Komisyonlari", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "631.01.001", "Trendyol Komisyon", AccountType.Expense, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "631.01.002", "Hepsiburada Komisyon", AccountType.Expense, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "631.01.003", "N11 Komisyon", AccountType.Expense, level: 3));
        accounts.Add(ChartOfAccounts.Create(tenantId, "631.02", "Kargo Giderleri", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "631.03", "Reklam ve Tanitim Giderleri", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "632", "Genel Yonetim Giderleri (-)", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "632.01", "Kira Gideri", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "632.02", "Personel Gideri", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "632.03", "Elektrik-Su-Dogalgaz", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "632.04", "Iletisim Gideri", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "632.05", "Yazilim ve Teknoloji Gideri", AccountType.Expense, level: 2));
        accounts.Add(ChartOfAccounts.Create(tenantId, "642", "Faiz Gelirleri", AccountType.Revenue, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "649", "Diger Olagan Gelir ve Karlar", AccountType.Revenue, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "660", "Kisa Vadeli Borc Giderleri (-)", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "689", "Diger Olagan Disi Gider ve Zararlar (-)", AccountType.Expense, level: 1));

        // ═══════════════════════════════════════
        // 7xx — MALIYET HESAPLARI (Expense)
        // ═══════════════════════════════════════
        accounts.Add(ChartOfAccounts.Create(tenantId, "710", "Direkt Ilk Madde ve Malzeme Giderleri", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "720", "Direkt Iscilik Giderleri", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "730", "Genel Uretim Giderleri", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "740", "Hizmet Uretim Maliyeti", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "770", "Genel Yonetim Giderleri", AccountType.Expense, level: 1));
        accounts.Add(ChartOfAccounts.Create(tenantId, "780", "Finansman Giderleri", AccountType.Expense, level: 1));

        return accounts.AsReadOnly();
    }
}
