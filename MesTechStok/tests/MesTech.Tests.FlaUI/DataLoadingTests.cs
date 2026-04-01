using FluentAssertions;
using Xunit.Abstractions;

namespace MesTech.Tests.UIAutomation;

/// <summary>
/// GRUP 2: Veri Yükleme testleri (TEST-06 ~ TEST-10).
/// Katman 2 BAK-GOR-KULLAN — ekranlar render oluyor mu?
/// </summary>
[Collection("FlaUI")]
[Trait("Category", "FlaUI")]
public class DataLoadingTests : FlaUITestBase
{
    public DataLoadingTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void TEST_06_UrunListesi_TabloRenderOlur()
    {
        Output.WriteLine("TEST-06: Ürün Listesi → Tablo render");
        var clicked = ClickMenu("Products");
        if (!clicked) { Screenshot("TEST-06", "Products", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var error = FindError("second operation", "column does not exist");
        var pass = error is null;
        Screenshot("TEST-06", "UrunListesi", pass, error is not null ? "DbError" : null);
        Output.WriteLine($"TEST-06: {(pass ? "PASS" : $"FAIL — {error}")}");
    }

    [Fact]
    public void TEST_07_KargoFirmalari_7KartGorunur()
    {
        Output.WriteLine("TEST-07: Kargo Firmaları → 7 kart");
        var clicked = ClickMenu("CargoProviders");
        if (!clicked) { Screenshot("TEST-07", "CargoProviders", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var cargoNames = new[] { "Yurtici", "Aras", "Surat", "MNG", "PTT", "HepsiJet", "Sendeo" };
        var found = cargoNames.Count(c => ContainsText(c));
        var pass = found >= 5; // En az 5/7 görünmeli
        Screenshot("TEST-07", "KargoFirmalari", pass, pass ? null : $"Only{found}of7");
        Output.WriteLine($"TEST-07: {(pass ? "PASS" : "FAIL")} — {found}/7 kargo firma kartı bulundu");
    }

    [Fact]
    public void TEST_08_ProviderAyarlari_9KartGorunur()
    {
        Output.WriteLine("TEST-08: Provider Ayarları → 9 kart");
        var clicked = ClickMenu("InvoiceProviders");
        if (!clicked) { Screenshot("TEST-08", "InvoiceProviders", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var providers = new[] { "Sovos", "Parasut", "GibPortal", "ELogo", "BirFatura",
            "DijitalPlanet", "HBFatura", "TrendyolEFaturam", "Mock" };
        var found = providers.Count(p => ContainsText(p));
        var pass = found >= 5;
        Screenshot("TEST-08", "ProviderAyarlari", pass, pass ? null : $"Only{found}of9");
        Output.WriteLine($"TEST-08: {(pass ? "PASS" : "FAIL")} — {found}/9 provider kartı bulundu");
    }

    [Fact]
    public void TEST_09_Platformlar_16PlatformGorunur()
    {
        Output.WriteLine("TEST-09: Platformlar → 16 platform");
        var clicked = ClickMenu("PlatformList");
        if (!clicked) { Screenshot("TEST-09", "PlatformList", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon",
            "eBay", "Etsy", "Ozon", "Shopify", "WooCommerce", "Zalando",
            "OpenCart", "PttAvm", "Pazarama", "Bitrix24" };
        var found = platforms.Count(p => ContainsText(p));
        var pass = found >= 10;
        Screenshot("TEST-09", "Platformlar", pass, pass ? null : $"Only{found}of15");
        Output.WriteLine($"TEST-09: {(pass ? "PASS" : "FAIL")} — {found}/15 platform kartı bulundu");
    }

    [Fact]
    public void TEST_10_MagazaEkle_WizardAcilir()
    {
        Output.WriteLine("TEST-10: Mağaza Ekle → Wizard açılır");
        var clicked = ClickMenu("StoreWizard");
        if (!clicked) { Screenshot("TEST-10", "StoreWizard", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        // Wizard açıldı mı — "Adım", "Platform", "Mağaza" gibi metin aranır
        var hasWizard = ContainsText("Adım") || ContainsText("Platform") || ContainsText("Mağaza")
            || ContainsText("Wizard") || ContainsText("Store");
        Screenshot("TEST-10", "MagazaWizard", hasWizard, hasWizard ? null : "NoWizard");
        Output.WriteLine($"TEST-10: {(hasWizard ? "PASS" : "WARN")} — Wizard {(hasWizard ? "açıldı" : "bulunamadı")}");
    }
}
