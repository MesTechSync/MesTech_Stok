using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Application.Features.Billing.Queries.GetBillingInvoices;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Common;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Invoice-related query handler null-guard and happy-path tests.
/// Covers: GetEInvoices, GetEInvoiceById, GetInvoices, GetInvoiceReport,
/// GetInvoiceProviders, GetBillingInvoices, GetSettlementBatches.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "InvoiceQueries")]
[Trait("Phase", "Dalga15")]
public class InvoiceQueryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly CancellationToken CT = CancellationToken.None;

    // ── GetEInvoicesHandler ──

    [Fact]
    public async Task GetEInvoicesHandler_NullRequest_Throws()
    {
        var sut = new GetEInvoicesHandler(
            new Mock<IEInvoiceDocumentRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetEInvoicesHandler_ValidRequest_ReturnsPaged()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        repo.Setup(r => r.GetPagedAsync(
                It.IsAny<MesTech.Domain.Enums.EInvoiceStatus?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>(), CT))
            .ReturnsAsync((new List<EInvoiceDocument>(), 0));

        var sut = new GetEInvoicesHandler(repo.Object);
        var result = await sut.Handle(new GetEInvoicesQuery(null, null, null, null), CT);

        result.Should().NotBeNull();
    }

    // ── GetEInvoiceByIdHandler ──

    [Fact]
    public async Task GetEInvoiceByIdHandler_NullRequest_Throws()
    {
        var sut = new GetEInvoiceByIdHandler(
            new Mock<IEInvoiceDocumentRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetEInvoiceByIdHandler_NotFound_ReturnsNull()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync((EInvoiceDocument?)null);

        var sut = new GetEInvoiceByIdHandler(repo.Object);
        var result = await sut.Handle(new GetEInvoiceByIdQuery(Guid.NewGuid()), CT);

        result.Should().BeNull();
    }

    // ── GetInvoicesHandler ──

    [Fact]
    public async Task GetInvoicesHandler_NullRequest_Throws()
    {
        var sut = new GetInvoicesHandler(
            new Mock<IInvoiceRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── GetInvoiceReportHandler ──

    [Fact]
    public async Task GetInvoiceReportHandler_NullRequest_Throws()
    {
        var sut = new GetInvoiceReportHandler(
            new Mock<IInvoiceRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    // ── GetInvoiceProvidersHandler ──

    [Fact]
    public async Task GetInvoiceProvidersHandler_NullRequest_Throws()
    {
        var sut = new GetInvoiceProvidersHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetInvoiceProvidersHandler_ReturnsProviderList()
    {
        var sut = new GetInvoiceProvidersHandler();
        var result = await sut.Handle(new GetInvoiceProvidersQuery(), CT);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    // ── GetBillingInvoicesHandler ──

    [Fact]
    public async Task GetBillingInvoicesHandler_NullRequest_Throws()
    {
        var sut = new GetBillingInvoicesHandler(
            new Mock<IBillingInvoiceRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetBillingInvoicesHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IBillingInvoiceRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Billing.BillingInvoice>());

        var sut = new GetBillingInvoicesHandler(repo.Object);
        var result = await sut.Handle(new GetBillingInvoicesQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetSettlementBatchesHandler ──

    [Fact]
    public async Task GetSettlementBatchesHandler_NullRequest_Throws()
    {
        var sut = new GetSettlementBatchesHandler(
            new Mock<ISettlementBatchRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetSettlementBatchesHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<ISettlementBatchRepository>();
        repo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.SettlementBatch>());

        var sut = new GetSettlementBatchesHandler(repo.Object);
        var result = await sut.Handle(new GetSettlementBatchesQuery(TenantId), CT);

        result.Should().BeEmpty();
    }
}
