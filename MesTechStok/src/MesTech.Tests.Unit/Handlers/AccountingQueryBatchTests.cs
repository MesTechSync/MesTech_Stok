using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Queries.GetFixedAssets;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using MesTech.Application.Features.Accounting.Queries.GetTaxSummary;
using MesTech.Application.Features.Accounting.Queries.ListFixedAssets;
using MesTech.Application.Features.Accounting.Queries.ListTaxWithholdings;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 5: Accounting handler batch tests — 15 handler
// Pattern: single-repo query handler → mock repo, verify call
// ════════════════════════════════════════════════════════

#region Tax

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetTaxRecordsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<ITaxRecordRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var sut = new GetTaxRecordsHandler(repo.Object);
        await sut.Handle(new GetTaxRecordsQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetTaxRecordByIdHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var sut = new GetTaxRecordByIdHandler(repo.Object);
        await sut.Handle(new GetTaxRecordByIdQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetTaxSummaryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldNotThrow()
    {
        var taxRepo = new Mock<ITaxRecordRepository>();
        var withholdingRepo = new Mock<ITaxWithholdingRepository>();
        taxRepo.Setup(r => r.GetByPeriodAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        withholdingRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new GetTaxSummaryHandler(taxRepo.Object, withholdingRepo.Object);
        var act = async () => await sut.Handle(new GetTaxSummaryQuery(Guid.NewGuid(), "2026-Q1"), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class ListTaxWithholdingsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<ITaxWithholdingRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new ListTaxWithholdingsHandler(repo.Object);
        await sut.Handle(new ListTaxWithholdingsQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Salary & Penalty

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetSalaryRecordsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new GetSalaryRecordsHandler(repo.Object);
        await sut.Handle(new GetSalaryRecordsQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetSalaryRecordByIdHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var sut = new GetSalaryRecordByIdHandler(repo.Object);
        await sut.Handle(new GetSalaryRecordByIdQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetPenaltyRecordsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<PenaltySource?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new GetPenaltyRecordsHandler(repo.Object);
        await sut.Handle(new GetPenaltyRecordsQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<PenaltySource?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetPenaltyRecordByIdHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var sut = new GetPenaltyRecordByIdHandler(repo.Object);
        await sut.Handle(new GetPenaltyRecordByIdQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Fixed Assets & Expenses

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetFixedAssetsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<IFixedAssetRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new GetFixedAssetsHandler(repo.Object);
        await sut.Handle(new GetFixedAssetsQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class ListFixedAssetsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<IFixedAssetRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new ListFixedAssetsHandler(repo.Object);
        await sut.Handle(new ListFixedAssetsQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetFixedExpenseByIdHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var sut = new GetFixedExpenseByIdHandler(repo.Object);
        await sut.Handle(new GetFixedExpenseByIdQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Settlement & Reconciliation

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetSettlementBatchesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<ISettlementBatchRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new GetSettlementBatchesHandler(repo.Object);
        await sut.Handle(new GetSettlementBatchesQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class GetReconciliationMatchesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepository()
    {
        var repo = new Mock<IReconciliationMatchRepository>();
        repo.Setup(r => r.GetByStatusAsync(It.IsAny<Guid>(), It.IsAny<ReconciliationStatus>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var sut = new GetReconciliationMatchesHandler(repo.Object);
        await sut.Handle(new GetReconciliationMatchesQuery(Guid.NewGuid()), CancellationToken.None);
        repo.Verify(r => r.GetByStatusAsync(It.IsAny<Guid>(), It.IsAny<ReconciliationStatus>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Update Commands

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class UpdateJournalEntryHandlerTests
{
    [Fact]
    public async Task Handle_NonExistent_ShouldReturnError()
    {
        var repo = new Mock<IJournalEntryRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.JournalEntry?)null);
        var sut = new UpdateJournalEntryHandler(repo.Object, uow.Object);
        var result = await sut.Handle(
            new UpdateJournalEntryCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "desc", null, new List<JournalLineInput>(), null),
            CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class UpdatePlatformCommissionRateHandlerTests
{
    [Fact]
    public async Task Handle_NonExistent_ShouldReturnError()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdatePlatformCommissionRateHandler(repo.Object, uow.Object);
        var result = await sut.Handle(
            new UpdatePlatformCommissionRateCommand(Guid.NewGuid(), Rate: 5.5m),
            CancellationToken.None);
        result.Should().BeFalse();
    }
}

#endregion
