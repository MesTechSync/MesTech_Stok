using FluentAssertions;
using MesTech.Tests.E2E.Base;

namespace MesTech.Tests.E2E.Tests;

[TestFixture]
[Category("E2E")]
public class FinanceTests : BlazorTestBase
{
    [Test]
    public async Task ProfitLoss_MonthNavigation_UpdatesPeriod()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BlazorBaseUrl}/finans/kar-zarar");
        await WaitForPageAsync();

        var periodText = await Page.Locator("small").First.TextContentAsync();

        await Page.ClickAsync("button:has-text('←')");
        await Page.WaitForTimeoutAsync(300);

        var newPeriodText = await Page.Locator("small").First.TextContentAsync();
        newPeriodText.Should().NotBe(periodText,
            "Onceki ay butonuna basinca periyot degismeli");
    }

    [Test]
    public async Task ProfitLoss_RevenueCards_AreVisible()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BlazorBaseUrl}/finans/kar-zarar");
        await WaitForPageAsync();

        var cards = new[] { "Toplam Gelir", "Toplam Gider", "Net Kar", "Kar Marji" };
        foreach (var cardTitle in cards)
        {
            var card = Page.Locator($"text={cardTitle}");
            (await card.IsVisibleAsync()).Should().BeTrue(
                $"'{cardTitle}' karti gorunmeli");
        }
    }

    [Test]
    public async Task ProfitLoss_NextMonth_DoesNotCrash()
    {
        await LoginAsync();
        await Page.GotoAsync($"{BlazorBaseUrl}/finans/kar-zarar");
        await WaitForPageAsync();

        await Page.ClickAsync("button:has-text('→')");
        await Page.WaitForTimeoutAsync(300);

        var header = Page.Locator("text=Kar / Zarar Raporu");
        (await header.IsVisibleAsync()).Should().BeTrue(
            "Ileri navigasyon sonrasi sayfa cokmemeli");
    }
}
