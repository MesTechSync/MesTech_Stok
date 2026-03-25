using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class AccountingExtraHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── GetFixedExpenseByIdHandler ───────────────────────────────

    [Fact]
    public async Task GetFixedExpenseById_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var sut = new GetFixedExpenseByIdHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetFixedExpenseById_NotFound_ReturnsNull()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedExpense?)null);

        var sut = new GetFixedExpenseByIdHandler(repo.Object);
        var query = new GetFixedExpenseByIdQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetPenaltyRecordsHandler ─────────────────────────────────

    [Fact]
    public async Task GetPenaltyRecords_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var sut = new GetPenaltyRecordsHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetPenaltyRecords_EmptyList_ReturnsEmpty()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PenaltyRecord>());

        var sut = new GetPenaltyRecordsHandler(repo.Object);
        var query = new GetPenaltyRecordsQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ── GetPenaltyRecordByIdHandler ──────────────────────────────

    [Fact]
    public async Task GetPenaltyRecordById_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var sut = new GetPenaltyRecordByIdHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetPenaltyRecordById_NotFound_ReturnsNull()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PenaltyRecord?)null);

        var sut = new GetPenaltyRecordByIdHandler(repo.Object);
        var query = new GetPenaltyRecordByIdQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetCashFlowTrendHandler ──────────────────────────────────

    [Fact]
    public async Task GetCashFlowTrend_EmptyData_ReturnsTrendWithZeros()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        var expenseRepo = new Mock<IExpenseRepository>();

        incomeRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Array.Empty<Income>());
        expenseRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Array.Empty<Expense>());

        var sut = new GetCashFlowTrendHandler(
            incomeRepo.Object, expenseRepo.Object,
            NullLogger<GetCashFlowTrendHandler>.Instance);

        var query = new GetCashFlowTrendQuery(_tenantId, 3);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Months.Should().HaveCount(3);
        result.CumulativeNet.Should().Be(0);
    }

    [Fact]
    public async Task GetCashFlowTrend_DefaultMonths_Returns6()
    {
        var incomeRepo = new Mock<IIncomeRepository>();
        var expenseRepo = new Mock<IExpenseRepository>();

        incomeRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Array.Empty<Income>());
        expenseRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync(Array.Empty<Expense>());

        var sut = new GetCashFlowTrendHandler(
            incomeRepo.Object, expenseRepo.Object,
            NullLogger<GetCashFlowTrendHandler>.Instance);

        var query = new GetCashFlowTrendQuery(_tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Months.Should().HaveCount(6);
    }
}
