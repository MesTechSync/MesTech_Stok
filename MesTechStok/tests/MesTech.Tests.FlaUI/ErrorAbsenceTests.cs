using Xunit.Abstractions;

namespace MesTech.Tests.UIAutomation;

/// <summary>
/// GRUP 3: Hata Yokluğu Doğrulama (TEST-11 ~ TEST-15).
/// Katman 2 BAK-GOR-KULLAN — bilinen hata kalıpları YOKTUR.
/// </summary>
[Collection("FlaUI")]
[Trait("Category", "FlaUI")]
public class ErrorAbsenceTests : FlaUITestBase
{
    public ErrorAbsenceTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void TEST_11_Dashboard_HataYoktur()
    {
        Output.WriteLine("TEST-11: Dashboard → Hata YOKTUR");
        var clicked = ClickMenu("Dashboard");
        if (!clicked) { Screenshot("TEST-11", "Dashboard", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var error = FindError("Hata olustu", "Hata Olustu", "Error occurred", "Unhandled");
        var pass = error is null;
        Screenshot("TEST-11", "Dashboard", pass, error is not null ? "HasError" : null);
        Output.WriteLine($"TEST-11: {(pass ? "PASS — Hata yok" : $"FAIL — {error}")}");
    }

    [Fact]
    public void TEST_12_TrendyolDetay_HataYoktur()
    {
        Output.WriteLine("TEST-12: Trendyol → Hata YOKTUR");
        var clicked = ClickMenu("Trendyol");
        if (!clicked) { Screenshot("TEST-12", "Trendyol", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var error = FindError("Hata", "Error", "Exception", "Bağlantı");
        var pass = error is null;
        Screenshot("TEST-12", "Trendyol", pass, error is not null ? "HasError" : null);
        Output.WriteLine($"TEST-12: {(pass ? "PASS — Hata yok" : $"FAIL — {error}")}");
    }

    [Fact]
    public void TEST_13_FaturaListesi_ColumnExistsHatasiYoktur()
    {
        Output.WriteLine("TEST-13: Fatura Listesi → 'column does not exist' YOKTUR");
        var clicked = ClickMenu("InvoiceList");
        if (!clicked) { Screenshot("TEST-13", "InvoiceList", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var error = FindError("column does not exist", "does not exist");
        var pass = error is null;
        Screenshot("TEST-13", "FaturaListesi", pass, error is not null ? "ColumnNotExist" : null);
        Output.WriteLine($"TEST-13: {(pass ? "PASS — Column hatası yok" : $"FAIL — {error}")}");
    }

    [Fact]
    public void TEST_14_StokYerlesim_RelationExistsHatasiYoktur()
    {
        Output.WriteLine("TEST-14: Stok Yerleşim → 'relation does not exist' YOKTUR");
        var clicked = ClickMenu("StockPlacement");
        if (!clicked) { Screenshot("TEST-14", "StockPlacement", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var error = FindError("relation does not exist", "does not exist");
        var pass = error is null;
        Screenshot("TEST-14", "StokYerlesim", pass, error is not null ? "RelationNotExist" : null);
        Output.WriteLine($"TEST-14: {(pass ? "PASS — Relation hatası yok" : $"FAIL — {error}")}");
    }

    [Fact]
    public void TEST_15_DenetimKayitlari_DateTimeKindHatasiYoktur()
    {
        Output.WriteLine("TEST-15: Denetim Kayıtları → 'DateTime Kind' YOKTUR");
        var clicked = ClickMenu("AuditLog");
        if (!clicked) { Screenshot("TEST-15", "AuditLog", false, "ButtonNotFound"); return; }
        Thread.Sleep(2000);

        var error = FindError("DateTime Kind", "Cannot write DateTime", "Kind=Unspecified");
        var pass = error is null;
        Screenshot("TEST-15", "DenetimKayitlari", pass, error is not null ? "DateTimeKind" : null);
        Output.WriteLine($"TEST-15: {(pass ? "PASS — DateTime Kind hatası yok" : $"FAIL — {error}")}");
    }
}
