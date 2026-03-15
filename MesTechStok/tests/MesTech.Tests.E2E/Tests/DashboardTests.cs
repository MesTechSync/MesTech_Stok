using FluentAssertions;
using MesTech.Tests.E2E.Base;

namespace MesTech.Tests.E2E.Tests;

[TestFixture]
[Category("E2E")]
public class DashboardTests : BlazorTestBase
{
    [Test]
    public async Task Dashboard_RevenueCard_ShowsNumericValue()
    {
        await LoginAsync();
        await WaitForPageAsync();

        var revenueValue = Page.Locator("text=Bu Ay Gelir")
                               .Locator("xpath=../div[contains(@class,'fs-3')]");
        await revenueValue.WaitForAsync(new() { Timeout = 5000 });

        var text = await revenueValue.TextContentAsync();
        text.Should().StartWith("₺", "Gelir ₺ ile baslamali");
    }

    [Test]
    public async Task Dashboard_QuickLinks_AllNavigate()
    {
        await LoginAsync();

        await Page.ClickAsync("a[href='/crm/leads']");
        await Page.WaitForURLAsync($"{BlazorBaseUrl}/crm/leads",
            new() { Timeout = 5000 });
        Page.Url.Should().Contain("/crm/leads");

        await Page.GoBackAsync();

        await Page.ClickAsync("a[href='/finans/kar-zarar']");
        await Page.WaitForURLAsync($"{BlazorBaseUrl}/finans/kar-zarar",
            new() { Timeout = 5000 });
        Page.Url.Should().Contain("/finans/kar-zarar");
    }

    [Test]
    public async Task Dashboard_Greeting_ChangesWithTime()
    {
        await LoginAsync();
        await WaitForPageAsync();

        var hour = DateTime.Now.Hour;
        var expectedKeyword = hour switch
        {
            < 12 => "Gunaydin",
            < 18 => "Iyi gunler",
            _    => "Iyi aksamlar"
        };

        var greeting = Page.Locator($"text*={expectedKeyword}");
        (await greeting.CountAsync()).Should().BeGreaterThan(0,
            $"Saat {hour}'de '{expectedKeyword}' selami gorunmeli");
    }
}
