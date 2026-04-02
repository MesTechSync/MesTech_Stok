using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;
using MesTech.Application.Features.Accounting.Queries.GetAccountBalance;
using MesTech.Application.Features.Accounting.Queries.GetBalanceSheet;
using MesTech.Application.Features.Accounting.Queries.GetBankTransactions;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Application.Features.Accounting.Queries.GetExpenseReport;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Features.Accounting.Queries.GetShipmentCosts;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// G28 kalan 15 accounting query handler testi.
/// [PAYLAŞIM-DEV5] DEV1 yazdı.
/// </summary>

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class CalculateDepreciationHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var svc = new DepreciationCalculationService();
        var sut = new CalculateDepreciationHandler(repo.Object, svc);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetAccountBalanceHandlerTests
{
    [Fact]
    public async Task Handle_AccountNotFound_ShouldReturnNull()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();
        accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);
        var sut = new GetAccountBalanceHandler(accountRepo.Object, journalRepo.Object);
        var result = await sut.Handle(new GetAccountBalanceQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetBalanceSheetHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();
        var sut = new GetBalanceSheetHandler(accountRepo.Object, journalRepo.Object);
        sut.Should().NotBeNull();
    }
}

// GetBankTransactionsHandlerTests — AccountingQueryTests2.cs'de mevcut

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetCashFlowReportHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var repo = new Mock<ICashFlowEntryRepository>();
        var sut = new GetCashFlowReportHandler(repo.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetChartOfAccountsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyList()
    {
        var repo = new Mock<IChartOfAccountsRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new GetChartOfAccountsHandler(repo.Object);
        var result = await sut.Handle(new GetChartOfAccountsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetCommissionSummaryHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var repo = new Mock<ICommissionRecordRepository>();
        var sut = new GetCommissionSummaryHandler(repo.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetCounterpartiesHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var repo = new Mock<ICounterpartyRepository>();
        var sut = new GetCounterpartiesHandler(repo.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetExpenseReportHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var repo = new Mock<IPersonalExpenseRepository>();
        var logger = new Mock<ILogger<GetExpenseReportHandler>>();
        var sut = new GetExpenseReportHandler(repo.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetJournalEntriesHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var repo = new Mock<IJournalEntryRepository>();
        var sut = new GetJournalEntriesHandler(repo.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetKdvReportHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptMediator()
    {
        var mediator = new Mock<ISender>();
        var sut = new GetKdvReportHandler(mediator.Object);
        sut.Should().NotBeNull();
    }
}

// GetPlatformCommissionRatesHandlerTests — AccountingQueryTests2.cs'de mevcut
// GetProfitReportHandlerTests — AccountingQueryTests2.cs'de mevcut

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetShipmentCostsHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var repo = new Mock<IShipmentCostRepository>();
        var logger = new Mock<ILogger<GetShipmentCostsHandler>>();
        var sut = new GetShipmentCostsHandler(repo.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetTrialBalanceHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var accountRepo = new Mock<IChartOfAccountsRepository>();
        var journalRepo = new Mock<IJournalEntryRepository>();
        var sut = new GetTrialBalanceHandler(accountRepo.Object, journalRepo.Object);
        sut.Should().NotBeNull();
    }
}
