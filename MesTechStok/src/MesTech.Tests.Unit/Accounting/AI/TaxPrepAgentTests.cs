using FluentAssertions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.AI.Accounting;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.AI;

/// <summary>
/// TaxPrepAgent tests — monthly tax draft report generation.
/// Verifies VAT calculation (391-191), withholding, stopaj, and disclaimer.
/// </summary>
[Trait("Category", "Unit")]
public class TaxPrepAgentTests
{
    private readonly Mock<IJournalEntryRepository> _journalRepoMock;
    private readonly Mock<IChartOfAccountsRepository> _chartRepoMock;
    private readonly Mock<ITaxWithholdingRepository> _taxWithholdingRepoMock;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly TaxPrepAgent _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    // Account IDs for standard accounts
    private readonly ChartOfAccounts _account391;
    private readonly ChartOfAccounts _account191;
    private readonly ChartOfAccounts _account360_02;
    private readonly ChartOfAccounts _account600;
    private readonly ChartOfAccounts _account150;

    public TaxPrepAgentTests()
    {
        _journalRepoMock = new Mock<IJournalEntryRepository>();
        _chartRepoMock = new Mock<IChartOfAccountsRepository>();
        _taxWithholdingRepoMock = new Mock<ITaxWithholdingRepository>();
        _tenantProviderMock = new Mock<ITenantProvider>();

        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);

        // Create standard accounts
        _account391 = ChartOfAccounts.Create(_tenantId, "391", "Hesaplanan KDV", AccountType.Liability);
        _account191 = ChartOfAccounts.Create(_tenantId, "191", "Indirilecek KDV", AccountType.Asset);
        _account360_02 = ChartOfAccounts.Create(_tenantId, "360.02", "Odenecek GV Stopaji", AccountType.Liability);
        _account600 = ChartOfAccounts.Create(_tenantId, "600", "Satis Geliri", AccountType.Revenue);
        _account150 = ChartOfAccounts.Create(_tenantId, "150", "Stoklar", AccountType.Asset);

        // Default chart setup
        _chartRepoMock.Setup(r => r.GetByCodeAsync(_tenantId, "391", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account391);
        _chartRepoMock.Setup(r => r.GetByCodeAsync(_tenantId, "191", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account191);
        _chartRepoMock.Setup(r => r.GetByCodeAsync(_tenantId, "360.02", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_account360_02);

        // Default: zero withholding
        _taxWithholdingRepoMock.Setup(r => r.GetTotalWithholdingAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Default: empty journal
        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry>());

        _sut = new TaxPrepAgent(
            _journalRepoMock.Object,
            _chartRepoMock.Object,
            _taxWithholdingRepoMock.Object,
            _tenantProviderMock.Object,
            new Mock<ILogger<TaxPrepAgent>>().Object);
    }

    private JournalEntry CreatePostedEntry(List<(Guid accountId, decimal debit, decimal credit)> lines)
    {
        var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Tax test entry");
        foreach (var (accountId, debit, credit) in lines)
        {
            entry.AddLine(accountId, debit, credit);
        }
        entry.Post();
        return entry;
    }

    // ── Core VAT Calculation ──

    [Fact]
    public async Task PrepareMonthlyTax_CalculatesVAT_391Minus191()
    {
        // Arrange — 391 credit (Hesaplanan KDV) = 1800, 191 debit (Indirilecek KDV) = 500
        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (_account391.Id, 0m, 1800m),  // Hesaplanan KDV (satistan alinan)
            (_account191.Id, 500m, 0m),    // Indirilecek KDV (alistan odenen)
            (Guid.NewGuid(), 1300m, 0m)    // Balancing
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.CalculatedVAT.Should().Be(1800m);
        result.DeductibleVAT.Should().Be(500m);
        result.PayableVAT.Should().Be(1300m); // 1800 - 500
    }

    [Fact]
    public async Task PrepareMonthlyTax_IncludesWithholding()
    {
        // Arrange
        _taxWithholdingRepoMock.Setup(r => r.GetTotalWithholdingAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(250m);

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.TotalWithholding.Should().Be(250m);
    }

    [Fact]
    public async Task PrepareMonthlyTax_NegativePayable_IsDevredenKDV()
    {
        // Arrange — 191 > 391 = Devreden KDV
        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (_account391.Id, 0m, 500m),   // 500 Hesaplanan
            (_account191.Id, 1200m, 0m),  // 1200 Indirilecek
            (Guid.NewGuid(), 0m, 700m)    // Balancing
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.PayableVAT.Should().BeNegative();
        result.Details.Should().Contain(d => d.Description.Contains("Devreden KDV"));
        // The devreden amount should be |payableVAT|
        var devredenLine = result.Details.FirstOrDefault(d => d.AccountCode == "190");
        devredenLine.Should().NotBeNull();
        devredenLine!.Amount.Should().Be(700m); // |500 - 1200| = 700
    }

    [Fact]
    public async Task PrepareMonthlyTax_AlwaysIncludesDisclaimer()
    {
        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.Disclaimer.Should().NotBeNullOrWhiteSpace();
        result.Disclaimer.Should().Contain("TASLAK");
        result.Disclaimer.Should().Contain("mali musavir");
    }

    [Fact]
    public async Task PrepareMonthlyTax_NoEntries_AllZeros()
    {
        // Arrange — default empty journal

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.CalculatedVAT.Should().Be(0m);
        result.DeductibleVAT.Should().Be(0m);
        result.PayableVAT.Should().Be(0m);
        result.TotalSales.Should().Be(0m);
        result.TotalPurchases.Should().Be(0m);
        result.TotalWithholding.Should().Be(0m);
        result.TotalStopaj.Should().Be(0m);
    }

    [Fact]
    public async Task PrepareMonthlyTax_OnlyPostedEntries()
    {
        // Arrange — one posted, one unposted
        var postedEntry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (_account391.Id, 0m, 1000m),
            (Guid.NewGuid(), 1000m, 0m)
        });

        var unpostedEntry = JournalEntry.Create(_tenantId, DateTime.UtcNow, "Unposted");
        unpostedEntry.AddLine(_account391.Id, 0m, 500m);
        unpostedEntry.AddLine(Guid.NewGuid(), 500m, 0m);
        // NOT posted

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { postedEntry, unpostedEntry });

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert — only posted entry's 391 credit counted
        result.CalculatedVAT.Should().Be(1000m);
    }

    // ── Theory: Various month/year combinations ──

    [Theory]
    [InlineData(2026, 1)]
    [InlineData(2026, 6)]
    [InlineData(2026, 12)]
    [InlineData(2025, 2)]
    [InlineData(2027, 7)]
    public async Task PrepareMonthlyTax_VariousMonthYear_SetsCorrectPeriod(int year, int month)
    {
        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, year, month);

        // Assert
        result.Year.Should().Be(year);
        result.Month.Should().Be(month);
    }

    [Fact]
    public async Task PrepareMonthlyTax_QueriesCorrectDateRange()
    {
        // Arrange
        var year = 2026;
        var month = 3;
        var expectedStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEnd = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        await _sut.PrepareMonthlyTaxAsync(_tenantId, year, month);

        // Assert
        _journalRepoMock.Verify(r => r.GetByDateRangeAsync(
            _tenantId, expectedStart, expectedEnd, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Stopaj ──

    [Fact]
    public async Task PrepareMonthlyTax_IncludesStopaj()
    {
        // Arrange — 360.02 credit entries (Stopaj)
        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (_account360_02.Id, 0m, 300m),
            (Guid.NewGuid(), 300m, 0m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.TotalStopaj.Should().Be(300m);
    }

    [Fact]
    public async Task PrepareMonthlyTax_Details_Has5StandardLines()
    {
        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert — at least 5 standard detail lines
        result.Details.Should().HaveCountGreaterOrEqualTo(5);
        result.Details.Should().Contain(d => d.AccountCode == "391");
        result.Details.Should().Contain(d => d.AccountCode == "191");
        result.Details.Should().Contain(d => d.AccountCode == "360.01");
        result.Details.Should().Contain(d => d.AccountCode == "360.02");
    }

    [Fact]
    public async Task PrepareMonthlyTax_MissingAccount391_CalculatedVATIsZero()
    {
        // Arrange — 391 account not found
        _chartRepoMock.Setup(r => r.GetByCodeAsync(_tenantId, "391", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.CalculatedVAT.Should().Be(0m);
    }

    [Fact]
    public async Task PrepareMonthlyTax_MissingAccount191_DeductibleVATIsZero()
    {
        // Arrange
        _chartRepoMock.Setup(r => r.GetByCodeAsync(_tenantId, "191", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.DeductibleVAT.Should().Be(0m);
    }

    [Fact]
    public async Task PrepareMonthlyTax_PositivePayableVAT_NoDevredenLine()
    {
        // Arrange — 391 > 191
        var entry = CreatePostedEntry(new List<(Guid, decimal, decimal)>
        {
            (_account391.Id, 0m, 2000m),
            (_account191.Id, 500m, 0m),
            (Guid.NewGuid(), 1500m, 0m)
        });

        _journalRepoMock.Setup(r => r.GetByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JournalEntry> { entry });

        // Act
        var result = await _sut.PrepareMonthlyTaxAsync(_tenantId, 2026, 3);

        // Assert
        result.PayableVAT.Should().BePositive();
        result.Details.Should().NotContain(d => d.AccountCode == "190");
    }
}
