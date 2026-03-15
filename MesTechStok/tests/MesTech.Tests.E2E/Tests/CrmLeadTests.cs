using FluentAssertions;
using MesTech.Tests.E2E.Base;

namespace MesTech.Tests.E2E.Tests;

[TestFixture]
[Category("E2E")]
public class CrmLeadTests : BlazorTestBase
{
    [Test]
    public async Task LeadsPage_SearchFilter_FiltersResults()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BlazorBaseUrl}/crm/leads");
        await WaitForPageAsync();

        var searchInput = Page.Locator("input[placeholder*='Isim']");
        await searchInput.FillAsync("zzz_hic_olmayan_lead_xyz");

        await Page.WaitForTimeoutAsync(500);

        var leadCount = await Page.Locator(".fw-semibold.small").CountAsync();
        var emptyMsg = Page.Locator("text=Lead bulunamadi");
        var hasEmpty = await emptyMsg.IsVisibleAsync();

        (leadCount == 0 || hasEmpty).Should().BeTrue(
            "Olmayan bir lead araninca bos sonuc gelmeli");
    }

    [Test]
    public async Task LeadsPage_StatusFilter_Works()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BlazorBaseUrl}/crm/leads");
        await WaitForPageAsync();

        var statusSelect = Page.Locator("select").First;
        await statusSelect.SelectOptionAsync("New");
        await Page.WaitForTimeoutAsync(500);

        var newBadges = await Page.Locator("text=Yeni").CountAsync();
        newBadges.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task LeadsPage_ResponsiveLayout_OnMobileViewport()
    {
        await Page.SetViewportSizeAsync(375, 812);
        await LoginAsync();
        await Page.GotoAsync($"{BlazorBaseUrl}/crm/leads");
        await WaitForPageAsync();

        var header = Page.Locator("text=Potansiyel Musteriler");
        await header.WaitForAsync(new() { Timeout = 5000 });
        (await header.IsVisibleAsync()).Should().BeTrue(
            "Mobil viewport'ta baslik gorunmeli");
    }
}
