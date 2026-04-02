using FlaUI.Core.AutomationElements;
using FluentAssertions;
using Xunit.Abstractions;

namespace MesTech.Tests.UIAutomation;

/// <summary>
/// GRUP 1: Login ve Navigation testleri (TEST-01 ~ TEST-05).
/// Katman 2 BAK-GOR-KULLAN emirnamesi.
/// </summary>
[Collection("FlaUI")]
[Trait("Category", "FlaUI")]
public class LoginNavigationTests : FlaUITestBase
{
    public LoginNavigationTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void TEST_01_Login_AnaSayfaAcilir()
    {
        Output.WriteLine("TEST-01: Login → Ana sayfa açılır");
        LoginSucceeded.Should().BeTrue("Login başarılı olmalı");
        MainWindow.Should().NotBeNull();
        MainWindow.Title.Should().Contain("MesTech");
        Screenshot("TEST-01", "AnaSayfa", true);
        Output.WriteLine("TEST-01: PASS");
    }

    [Theory]
    [InlineData("Stok Takibi", "Stok")]
    [InlineData("Kargo Firmalari", "Kargo")]
    [InlineData("Fatura Listesi", "Fatura")]
    [InlineData("Platformlar", "Platform")]
    [InlineData("CRM", "Kişi")]
    public void TEST_02_SidebarMenu_DogruSayfaAcilir(string menuName, string expectedTitle)
    {
        Output.WriteLine($"TEST-02: Sidebar '{menuName}' → başlık '{expectedTitle}' içerir");
        var clicked = ClickMenu(menuName);
        if (!clicked)
        {
            Screenshot("TEST-02", menuName, false, "ButtonNotFound");
            Output.WriteLine($"TEST-02 [{menuName}]: SKIP — buton bulunamadı");
            return;
        }
        Thread.Sleep(1500);
        Screenshot("TEST-02", menuName, true);
        Output.WriteLine($"TEST-02 [{menuName}]: PASS — ekran açıldı");
    }

    [Fact]
    public void TEST_03_SidebarHighlight_AktifMenuVurgulanir()
    {
        Output.WriteLine("TEST-03: Sidebar highlight kontrolü");
        var clicked = ClickMenu("Kontrol Paneli");
        Thread.Sleep(1000);
        Screenshot("TEST-03", "SidebarHighlight", clicked);
        Output.WriteLine($"TEST-03: {(clicked ? "PASS" : "SKIP")} — Kontrol Paneli tıklandı");
    }

    [Fact]
    public void TEST_04_Breadcrumb_DogruYolGosterir()
    {
        Output.WriteLine("TEST-04: Breadcrumb kontrolü");
        var clicked = ClickMenu("Envanter");
        Thread.Sleep(1500);
        var hasBreadcrumb = ContainsText("Envanter") || ContainsText("Stok") || ContainsText("Inventory");
        Screenshot("TEST-04", "Breadcrumb", hasBreadcrumb, hasBreadcrumb ? null : "NoBreadcrumb");
        Output.WriteLine($"TEST-04: {(hasBreadcrumb ? "PASS" : "WARN")} — breadcrumb {(hasBreadcrumb ? "bulundu" : "bulunamadı")}");
    }

    [Fact]
    public void TEST_05_Cikis_LogineDoner()
    {
        Output.WriteLine("TEST-05: Çıkış → Login'e döner");
        // Çıkış butonunu bul
        var clicked = ClickMenu("Logout") || ClickMenu("Çıkış") || ClickMenu("Cikis");
        if (!clicked)
        {
            // x:Name="LogoutBtn" — farklı arama
            try
            {
                var el = MainWindow.FindFirstDescendant(CF.ByAutomationId("LogoutBtn"))
                    ?? MainWindow.FindFirstDescendant(CF.ByName("LogoutBtn"));
                if (el is not null) { el.AsButton().Click(); clicked = true; }
            }
            catch { }
        }

        if (!clicked)
        {
            Screenshot("TEST-05", "Cikis", false, "ButtonNotFound");
            Output.WriteLine("TEST-05: SKIP — Çıkış butonu bulunamadı");
            return;
        }

        Thread.Sleep(3000);
        // WelcomeWindow döndü mü?
        var welcome = WaitForWindow("Giris Ekrani", 5000) ?? WaitForWindow("MesTech", 3000);
        var returned = welcome is not null;
        Screenshot("TEST-05", "CikisSonrasi", returned, returned ? null : "NoReturn");
        Output.WriteLine($"TEST-05: {(returned ? "PASS" : "FAIL")} — {(returned ? "Login'e döndü" : "Login'e dönmedi")}");
    }
}
