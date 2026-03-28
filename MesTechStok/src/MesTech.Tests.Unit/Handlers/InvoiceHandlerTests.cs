using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for invoice handlers — Approve, BulkCreate, CreateEInvoice,
/// CancelEInvoice, SendEInvoice, CreateBillingInvoice.
/// </summary>
[Trait("Category", "Unit")]
public class InvoiceHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ ApproveInvoiceHandler ═══════

    [Fact]
    public async Task ApproveInvoice_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<ApproveInvoiceHandler>>();
        var sut = new ApproveInvoiceHandler(repo.Object, logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveInvoice_NotFound_ReturnsFalse()
    {
        var repo = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<ApproveInvoiceHandler>>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((global::MesTech.Domain.Entities.Invoice?)null);

        var sut = new ApproveInvoiceHandler(repo.Object, logger.Object);
        var cmd = new ApproveInvoiceCommand(Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    // ═══════ BulkCreateInvoiceHandler ═══════

    [Fact]
    public async Task BulkCreateInvoice_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<BulkCreateInvoiceHandler>>();
        var sut = new BulkCreateInvoiceHandler(repo.Object, Mock.Of<IOrderRepository>(), Mock.Of<IUnitOfWork>(), logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task BulkCreateInvoice_EmptyList_ReturnsZeroCounts()
    {
        var repo = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<BulkCreateInvoiceHandler>>();
        var sut = new BulkCreateInvoiceHandler(repo.Object, Mock.Of<IOrderRepository>(), Mock.Of<IUnitOfWork>(), logger.Object);
        var cmd = new BulkCreateInvoiceCommand(new List<Guid>(), InvoiceProvider.Sovos);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(0);
        result.FailCount.Should().Be(0);
    }

    [Fact]
    public async Task BulkCreateInvoice_WithOrderIds_ReturnsSuccessCounts()
    {
        var repo = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<BulkCreateInvoiceHandler>>();
        var sut = new BulkCreateInvoiceHandler(repo.Object, Mock.Of<IOrderRepository>(), Mock.Of<IUnitOfWork>(), logger.Object);
        var orderIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var cmd = new BulkCreateInvoiceCommand(orderIds, InvoiceProvider.Sovos);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalRequested.Should().Be(2);
        result.SuccessCount.Should().Be(2);
    }

    // ═══════ CreateEInvoiceHandler ═══════

    [Fact]
    public async Task CreateEInvoice_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var sut = new CreateEInvoiceHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEInvoice_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var sut = new CreateEInvoiceHandler(repo.Object);
        var lines = new List<CreateEInvoiceLineRequest>
        {
            new("Urun A", 2m, "C62", 100m, 20, 0m, null)
        };
        var cmd = new CreateEInvoiceCommand(
            null, "1234567890", "Alici A.S.", null,
            EInvoiceScenario.TEMELFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, "TRY", lines, "sovos");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ CancelEInvoiceHandler ═══════

    [Fact]
    public async Task CancelEInvoice_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var provider = new Mock<IEInvoiceProvider>();
        var sut = new CancelEInvoiceHandler(repo.Object, provider.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CancelEInvoice_NotFound_ReturnsFalse()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var provider = new Mock<IEInvoiceProvider>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EInvoiceDocument?)null);

        var sut = new CancelEInvoiceHandler(repo.Object, provider.Object);
        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), "Iptal sebebi");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    // ═══════ SendEInvoiceHandler ═══════

    [Fact]
    public async Task SendEInvoice_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var provider = new Mock<IEInvoiceProvider>();
        var logger = new Mock<ILogger<SendEInvoiceHandler>>();
        var sut = new SendEInvoiceHandler(repo.Object, provider.Object, logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SendEInvoice_NotFound_ReturnsFalse()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var provider = new Mock<IEInvoiceProvider>();
        var logger = new Mock<ILogger<SendEInvoiceHandler>>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EInvoiceDocument?)null);

        var sut = new SendEInvoiceHandler(repo.Object, provider.Object, logger.Object);
        var cmd = new SendEInvoiceCommand(Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    // ═══════ CreateBillingInvoiceHandler ═══════

    [Fact]
    public async Task CreateBillingInvoice_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IBillingInvoiceRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateBillingInvoiceHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBillingInvoice_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IBillingInvoiceRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetNextSequenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var sut = new CreateBillingInvoiceHandler(repo.Object, uow.Object);
        var cmd = new CreateBillingInvoiceCommand(_tenantId, Guid.NewGuid(), 199.99m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<global::MesTech.Domain.Entities.Billing.BillingInvoice>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
