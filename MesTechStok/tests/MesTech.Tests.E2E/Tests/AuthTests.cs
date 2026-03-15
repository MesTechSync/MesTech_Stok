using FluentAssertions;
using MesTech.Tests.E2E.Base;

namespace MesTech.Tests.E2E.Tests;

[TestFixture]
[Category("E2E")]
public class AuthTests : BlazorTestBase
{
    [Test]
    public async Task Login_ValidCredentials_RedirectsToDashboard()
    {
        await Page.GotoAsync($"{BlazorBaseUrl}/giris");
        await WaitForPageAsync();

        await Page.FillAsync(
            "input[placeholder='kullanici.adi'], input[name='username'], input[type='text']",
            TestUsername);
        await Page.FillAsync("input[type='password']", TestPassword);
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForURLAsync($"{BlazorBaseUrl}/**",
            new() { Timeout = 8000 });
    }

    [Test]
    public async Task Login_WrongPassword_ShowsError()
    {
        await Page.GotoAsync($"{BlazorBaseUrl}/giris");
        await WaitForPageAsync();

        await Page.FillAsync(
            "input[placeholder='kullanici.adi'], input[name='username'], input[type='text']",
            "admin");
        await Page.FillAsync("input[type='password']", "yanlis_sifre_12345");
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForTimeoutAsync(1000);

        var errorMsg = Page.Locator(".alert-danger");
        await errorMsg.WaitForAsync(new() { Timeout = 3000 });
        (await errorMsg.IsVisibleAsync()).Should().BeTrue(
            "Yanlis sifrede hata mesaji gorunmeli");
    }

    [Test]
    public async Task ProtectedPage_WithoutLogin_RedirectsToLogin()
    {
        await Page.GotoAsync($"{BlazorBaseUrl}/crm/leads");
        await Page.WaitForLoadStateAsync();

        await Page.WaitForURLAsync($"**giris**",
            new() { Timeout = 5000 });
        Page.Url.Should().Contain("giris",
            "Auth olmadan korunan sayfaya erisim login'e yonlendirmeli");
    }
}
