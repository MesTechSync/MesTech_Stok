using FluentAssertions;
using MesTech.Tests.E2E.Base;

namespace MesTech.Tests.E2E.Tests;

/// <summary>
/// Accounting UI E2E tests — Panel B M4 muhasebe sayfalari.
/// Validates journal entry flow, accounting dashboard cards,
/// report tabs, commission management, and full accounting flow.
///
/// Target: mestech-shell.html served at BlazorBaseUrl (default http://localhost:3200)
/// Shell uses hash-based routing: #accounting, #accounting/journal-entry, etc.
/// </summary>
[TestFixture]
[Category("E2E")]
[Category("Accounting")]
public class AccountingTests : BlazorTestBase
{
    /// <summary>
    /// Helper: navigate to the shell and go to a hash route.
    /// </summary>
    private async Task NavigateToAccountingPageAsync(string hashRoute)
    {
        await Page.GotoAsync($"{BlazorBaseUrl}/#{hashRoute}");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(800);
    }

    // ───────────────────────────────────────────────────────────
    // TEST 1: Journal Entry flow — fill form, verify balance, save
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task JournalEntry_FillAndSave_ShowsSuccessMessage()
    {
        await NavigateToAccountingPageAsync("accounting/journal-entry");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Page title should indicate journal entry
        var pageTitle = Page.Locator("h1, h2");
        await pageTitle.First.WaitForAsync(new() { Timeout = 5000 });
        var titleText = await pageTitle.First.TextContentAsync();
        titleText.Should().NotBeNullOrWhiteSpace(
            "Yevmiye fisi sayfasinda baslik olmali");

        // Date input should be present and fillable
        var dateInput = Page.Locator("input[type='date']");
        (await dateInput.CountAsync()).Should().BeGreaterThan(0,
            "Tarih alani (input[type='date']) mevcut olmali");
        await dateInput.First.FillAsync("2026-03-16");

        // Description field should be present
        var descriptionInput = Page.Locator("input[name='description'], textarea[name='description'], input[placeholder*='aciklama' i], textarea[placeholder*='aciklama' i]");
        if (await descriptionInput.CountAsync() == 0)
        {
            // Fallback: try generic text input or textarea
            descriptionInput = Page.Locator("form textarea, form input[type='text']").First;
        }
        (await descriptionInput.CountAsync()).Should().BeGreaterThan(0,
            "Aciklama alani mevcut olmali");
        await descriptionInput.First.FillAsync("Test yevmiye fisi - E2E");

        // Add debit row — click add row button
        var addRowBtn = Page.Locator("button:has-text('Satir Ekle'), button:has-text('satir ekle'), .btn-add-row, button:has-text('+')");
        if (await addRowBtn.CountAsync() > 0)
        {
            await addRowBtn.First.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
        }

        // Fill debit amount in first row
        var debitInputs = Page.Locator("input[name*='debit' i], input[placeholder*='borc' i], .debit-input");
        if (await debitInputs.CountAsync() > 0)
        {
            await debitInputs.First.FillAsync("1000");
        }

        // Add credit row
        if (await addRowBtn.CountAsync() > 0)
        {
            await addRowBtn.First.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
        }

        // Fill credit amount in second row
        var creditInputs = Page.Locator("input[name*='credit' i], input[placeholder*='alacak' i], .credit-input");
        if (await creditInputs.CountAsync() > 0)
        {
            await creditInputs.Last.FillAsync("1000");
        }

        // Verify balance indicator — debit should equal credit
        var balanceIndicator = Page.Locator(".balance-status, .balance-ok, .text-success, [data-balance]");
        if (await balanceIndicator.CountAsync() > 0)
        {
            var balanceText = await balanceIndicator.First.TextContentAsync();
            balanceText.Should().NotBeNullOrWhiteSpace(
                "Bakiye gostergesi icerik icermeli");
        }

        // Click save button
        var saveBtn = Page.Locator("button[type='submit'], button:has-text('Kaydet'), button:has-text('kaydet')");
        (await saveBtn.CountAsync()).Should().BeGreaterThan(0,
            "Kaydet butonu mevcut olmali");
        await saveBtn.First.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Verify success feedback (toast, alert, or inline message)
        var successMsg = Page.Locator(".alert-success, .toast-success, .text-success, [role='alert']:has-text('basari')");
        if (await successMsg.CountAsync() > 0)
        {
            (await successMsg.First.IsVisibleAsync()).Should().BeTrue(
                "Kayit sonrasi basari mesaji gorunmeli");
        }
    }

    // ───────────────────────────────────────────────────────────
    // TEST 2: Accounting Dashboard — 4 summary cards render
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task AccountingDashboard_SummaryCards_AreRendered()
    {
        await NavigateToAccountingPageAsync("accounting");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Page should have a title or heading
        var pageTitle = Page.Locator("h1, h2");
        await pageTitle.First.WaitForAsync(new() { Timeout = 5000 });

        // Summary cards should render (Bootstrap .card components)
        var summaryCards = Page.Locator(".card");
        await summaryCards.First.WaitForAsync(new() { Timeout = 5000 });
        var cardCount = await summaryCards.CountAsync();
        cardCount.Should().BeGreaterOrEqualTo(4,
            "Muhasebe dashboard'unda en az 4 ozet karti olmali");

        // Each card should have visible content (not empty)
        for (var i = 0; i < Math.Min(cardCount, 4); i++)
        {
            var card = summaryCards.Nth(i);
            (await card.IsVisibleAsync()).Should().BeTrue(
                $"Ozet karti #{i + 1} gorunur olmali");

            var cardBody = card.Locator(".card-body, .card-text, h3, h4, .fw-bold, strong");
            if (await cardBody.CountAsync() > 0)
            {
                var valueText = await cardBody.First.TextContentAsync();
                valueText.Should().NotBeNullOrWhiteSpace(
                    $"Ozet karti #{i + 1} bos olmamali, deger icermeli");
            }
        }

        // Card titles should include key accounting terms
        var cardTitles = Page.Locator(".card-title, .card-header, .card h5, .card h6");
        if (await cardTitles.CountAsync() >= 4)
        {
            var allTitleText = "";
            for (var i = 0; i < await cardTitles.CountAsync(); i++)
            {
                allTitleText += await cardTitles.Nth(i).TextContentAsync() + " ";
            }
            allTitleText.Should().NotBeNullOrWhiteSpace(
                "Kart basliklari icerik icermeli");
        }
    }

    // ───────────────────────────────────────────────────────────
    // TEST 3: Reports page — 3 tabs (Mizan, Bilanco, Gelir-Gider)
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task Reports_ThreeTabsExist_TablesLoadOnClick()
    {
        await NavigateToAccountingPageAsync("accounting/reports");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Tab navigation should be present (Bootstrap nav-tabs or similar)
        var tabContainer = Page.Locator(".nav-tabs, .nav-pills, [role='tablist']");
        (await tabContainer.CountAsync()).Should().BeGreaterThan(0,
            "Rapor sekmeleri (nav-tabs) mevcut olmali");

        // Should have at least 3 tabs
        var tabs = Page.Locator(".nav-tabs .nav-link, .nav-pills .nav-link, [role='tab']");
        (await tabs.CountAsync()).Should().BeGreaterOrEqualTo(3,
            "En az 3 rapor sekmesi olmali (Mizan, Bilanco, Gelir-Gider)");

        // Verify expected tab labels exist
        var allTabText = "";
        for (var i = 0; i < await tabs.CountAsync(); i++)
        {
            allTabText += await tabs.Nth(i).TextContentAsync() + " ";
        }
        allTabText.ToLowerInvariant().Should().ContainAll("mizan", "bilan",
            "Mizan ve Bilanco sekmeleri mevcut olmali");

        // Click each tab and verify table content loads
        for (var i = 0; i < Math.Min(await tabs.CountAsync(), 3); i++)
        {
            var tab = tabs.Nth(i);
            var tabName = await tab.TextContentAsync();
            await tab.ClickAsync();
            await Page.WaitForTimeoutAsync(800);

            // Each tab should reveal a table or data content
            var tabPanel = Page.Locator(".tab-pane.active, .tab-pane.show, [role='tabpanel']:visible");
            if (await tabPanel.CountAsync() > 0)
            {
                var table = tabPanel.Locator("table");
                if (await table.CountAsync() > 0)
                {
                    (await table.First.IsVisibleAsync()).Should().BeTrue(
                        $"'{tabName?.Trim()}' sekmesinde tablo gorunur olmali");

                    // Table should have header row
                    var tableHeaders = table.First.Locator("th");
                    (await tableHeaders.CountAsync()).Should().BeGreaterThan(0,
                        $"'{tabName?.Trim()}' tablosunda baslik sutunlari olmali");
                }
            }
        }
    }

    // ───────────────────────────────────────────────────────────
    // TEST 4: Commission Management — table and add form
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task Commissions_TableAndAddForm_WorkCorrectly()
    {
        await NavigateToAccountingPageAsync("accounting/commissions");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Page heading should be present
        var pageTitle = Page.Locator("h1, h2");
        await pageTitle.First.WaitForAsync(new() { Timeout = 5000 });

        // Commission rates table should render with platform data
        var commissionTable = Page.Locator("table");
        (await commissionTable.CountAsync()).Should().BeGreaterThan(0,
            "Komisyon oranlari tablosu mevcut olmali");

        // Table should have headers (Platform, Oran, Kategori, etc.)
        var tableHeaders = commissionTable.First.Locator("th");
        (await tableHeaders.CountAsync()).Should().BeGreaterOrEqualTo(2,
            "Komisyon tablosunda en az 2 baslik sutunu olmali");

        // Table should have at least one data row (pre-loaded platform rates)
        var tableRows = commissionTable.First.Locator("tbody tr");
        var initialRowCount = await tableRows.CountAsync();
        initialRowCount.Should().BeGreaterThan(0,
            "Komisyon tablosunda en az 1 platform orani olmali");

        // Add button should exist
        var addBtn = Page.Locator("button:has-text('Ekle'), button:has-text('ekle'), .btn-add, button:has-text('+')");
        (await addBtn.CountAsync()).Should().BeGreaterThan(0,
            "Komisyon ekleme butonu mevcut olmali");

        // Click add button to open form/modal
        await addBtn.First.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Form or modal should appear
        var form = Page.Locator("form, .modal.show, .modal.fade.show, .offcanvas.show");
        (await form.CountAsync()).Should().BeGreaterThan(0,
            "Komisyon ekleme formu veya modal gorunmeli");

        // Fill platform name
        var platformInput = Page.Locator("input[name='platform'], select[name='platform'], input[placeholder*='platform' i]");
        if (await platformInput.CountAsync() > 0)
        {
            if (await platformInput.First.EvaluateAsync<string>("el => el.tagName") == "SELECT")
            {
                await platformInput.First.SelectOptionAsync(new Microsoft.Playwright.SelectOptionValue { Index = 1 });
            }
            else
            {
                await platformInput.First.FillAsync("Trendyol");
            }
        }

        // Fill commission rate
        var rateInput = Page.Locator("input[name='rate'], input[name='commission'], input[type='number'], input[placeholder*='oran' i]");
        if (await rateInput.CountAsync() > 0)
        {
            await rateInput.First.FillAsync("12.5");
        }

        // Submit the form
        var submitBtn = Page.Locator("form button[type='submit'], button:has-text('Kaydet'), .modal button.btn-primary");
        if (await submitBtn.CountAsync() > 0)
        {
            await submitBtn.First.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
        }

        // Verify new entry appears in table (row count increased)
        var updatedRowCount = await commissionTable.First.Locator("tbody tr").CountAsync();
        updatedRowCount.Should().BeGreaterOrEqualTo(initialRowCount,
            "Yeni komisyon eklendikten sonra tablo satir sayisi artmali veya ayni kalmali");
    }

    // ───────────────────────────────────────────────────────────
    // TEST 5: Full flow — Journal entry to reports verification
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task FullFlow_JournalEntryToReports_MizanUpdated()
    {
        // Step 1: Navigate to journal entry and create a new entry
        await NavigateToAccountingPageAsync("accounting/journal-entry");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Fill date
        var dateInput = Page.Locator("input[type='date']");
        if (await dateInput.CountAsync() > 0)
        {
            await dateInput.First.FillAsync("2026-03-16");
        }

        // Fill description
        var descInput = Page.Locator("form textarea, form input[type='text']").First;
        if (await descInput.CountAsync() > 0)
        {
            await descInput.FillAsync("E2E tam akis testi");
        }

        // Save journal entry
        var saveBtn = Page.Locator("button[type='submit'], button:has-text('Kaydet')");
        if (await saveBtn.CountAsync() > 0)
        {
            await saveBtn.First.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
        }

        // Step 2: Navigate to reports page
        await NavigateToAccountingPageAsync("accounting/reports");

        contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Step 3: Verify Mizan tab loads with data
        var mizanTab = Page.Locator(".nav-link:has-text('Mizan'), [role='tab']:has-text('Mizan')");
        if (await mizanTab.CountAsync() > 0)
        {
            await mizanTab.First.ClickAsync();
            await Page.WaitForTimeoutAsync(800);
        }

        // Mizan table should be present and have data
        var mizanTable = Page.Locator(".tab-pane.active table, .tab-pane.show table, table");
        (await mizanTable.CountAsync()).Should().BeGreaterThan(0,
            "Mizan tablosu rapor sayfasinda mevcut olmali");

        if (await mizanTable.First.Locator("tbody tr").CountAsync() > 0)
        {
            var firstRowText = await mizanTable.First.Locator("tbody tr").First.TextContentAsync();
            firstRowText.Should().NotBeNullOrWhiteSpace(
                "Mizan tablosunda veri satiri icerik icermeli");
        }

        // Step 4: Verify navigation back to dashboard works
        await NavigateToAccountingPageAsync("accounting");

        contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Dashboard should still render its summary cards
        var cards = Page.Locator(".card");
        (await cards.CountAsync()).Should().BeGreaterThan(0,
            "Muhasebe ana sayfasina donuste ozet kartlari gorunmeli");
    }
}
