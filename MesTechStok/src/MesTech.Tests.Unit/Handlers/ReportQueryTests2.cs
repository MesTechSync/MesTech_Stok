using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using MesTech.Application.Features.Accounting.Queries.GetTaxSummary;
using MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;
using MesTech.Application.Features.Dashboard.Queries.GetRevenueChart;
using MesTech.Application.Features.Reporting.Queries.GetSavedReports;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Report-related query handler null-guard and happy-path tests (batch 2).
/// Covers: GetRevenueChart, GetCashFlowReport, GetTaxSummary, GetTaxRecords,
/// GetTaxRecordById, GetWithholdingRates, GetKdvDeclarationDraft,
/// GetSalaryRecords, GetSalaryRecordById, GetSavedReports.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ReportQueries2")]
[Trait("Phase", "Dalga15")]
public class ReportQueryTests2
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly CancellationToken CT = CancellationToken.None;

    // ── GetRevenueChartHandler ──

    [Fact]
    public async Task GetRevenueChartHandler_NullRequest_Throws()
    {
        var sut = new GetRevenueChartHandler(
            new Mock<IOrderRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── GetCashFlowReportHandler ──

    [Fact]
    public async Task GetCashFlowReportHandler_NullRequest_Throws()
    {
        var sut = new GetCashFlowReportHandler(
            new Mock<ICashFlowEntryRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── GetTaxSummaryHandler ──

    [Fact]
    public async Task GetTaxSummaryHandler_NullRequest_Throws()
    {
        var sut = new GetTaxSummaryHandler(
            new Mock<ITaxRecordRepository>().Object,
            new Mock<ITaxWithholdingRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── GetTaxRecordsHandler ──

    [Fact]
    public async Task GetTaxRecordsHandler_NullRequest_Throws()
    {
        var sut = new GetTaxRecordsHandler(
            new Mock<ITaxRecordRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetTaxRecordsHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<ITaxRecordRepository>();
        repo.Setup(r => r.GetAllAsync(
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<int?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.TaxRecord>());

        var sut = new GetTaxRecordsHandler(repo.Object);
        var result = await sut.Handle(new GetTaxRecordsQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetTaxRecordByIdHandler ──

    [Fact]
    public async Task GetTaxRecordByIdHandler_NullRequest_Throws()
    {
        var sut = new GetTaxRecordByIdHandler(
            new Mock<ITaxRecordRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetTaxRecordByIdHandler_NotFound_ReturnsNull()
    {
        var repo = new Mock<ITaxRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.TaxRecord?)null);

        var sut = new GetTaxRecordByIdHandler(repo.Object);
        var result = await sut.Handle(new GetTaxRecordByIdQuery(Guid.NewGuid()), CT);

        result.Should().BeNull();
    }

    // ── GetWithholdingRatesHandler ──

    [Fact]
    public async Task GetWithholdingRatesHandler_NullRequest_Throws()
    {
        var sut = new GetWithholdingRatesHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetWithholdingRatesHandler_ReturnsRateList()
    {
        var sut = new GetWithholdingRatesHandler();
        var result = await sut.Handle(new GetWithholdingRatesQuery(), CT);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    // ── GetKdvDeclarationDraftHandler ──

    [Fact]
    public void GetKdvDeclarationDraftHandler_CanBeConstructed()
    {
        var sut = new GetKdvDeclarationDraftHandler(
            new Mock<ITaxRecordRepository>().Object,
            new Mock<ICommissionRecordRepository>().Object,
            new Mock<ITaxWithholdingRepository>().Object);

        sut.Should().NotBeNull();
    }

    // ── GetSalaryRecordsHandler ──

    [Fact]
    public async Task GetSalaryRecordsHandler_NullRequest_Throws()
    {
        var sut = new GetSalaryRecordsHandler(
            new Mock<ISalaryRecordRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetSalaryRecordsHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        repo.Setup(r => r.GetAllAsync(
                It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.SalaryRecord>());

        var sut = new GetSalaryRecordsHandler(repo.Object);
        var result = await sut.Handle(new GetSalaryRecordsQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetSalaryRecordByIdHandler ──

    [Fact]
    public async Task GetSalaryRecordByIdHandler_NullRequest_Throws()
    {
        var sut = new GetSalaryRecordByIdHandler(
            new Mock<ISalaryRecordRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetSalaryRecordByIdHandler_NotFound_ReturnsNull()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync((MesTech.Domain.Accounting.Entities.SalaryRecord?)null);

        var sut = new GetSalaryRecordByIdHandler(repo.Object);
        var result = await sut.Handle(new GetSalaryRecordByIdQuery(Guid.NewGuid()), CT);

        result.Should().BeNull();
    }

    // ── GetSavedReportsHandler ──

    [Fact]
    public async Task GetSavedReportsHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<ISavedReportRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Reporting.SavedReport>());

        var sut = new GetSavedReportsHandler(
            repo.Object,
            new Mock<ILogger<GetSavedReportsHandler>>().Object);

        var result = await sut.Handle(new GetSavedReportsQuery(TenantId), CT);
        result.Should().BeEmpty();
    }
}
