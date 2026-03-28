using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Invoice")]
[Trait("Group", "Handler")]
public class EInvoiceHandlerTests
{
    // ═══ CreateEInvoice ═══

    [Fact]
    public async Task CreateEInvoice_NullRequest_Throws()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var handler = new CreateEInvoiceHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ SendEInvoice ═══

    [Fact]
    public async Task SendEInvoice_NullRequest_Throws()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var provider = new Mock<IEInvoiceProvider>();
        var logger = Mock.Of<ILogger<SendEInvoiceHandler>>();
        var handler = new SendEInvoiceHandler(repo.Object, provider.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CancelEInvoice ═══

    [Fact]
    public async Task CancelEInvoice_NullRequest_Throws()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var provider = new Mock<IEInvoiceProvider>();
        var handler = new CancelEInvoiceHandler(repo.Object, provider.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetEInvoices ═══

    [Fact]
    public async Task GetEInvoices_NullRequest_Throws()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var handler = new GetEInvoicesHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetEInvoiceById ═══

    [Fact]
    public async Task GetEInvoiceById_NullRequest_Throws()
    {
        var repo = new Mock<IEInvoiceDocumentRepository>();
        var handler = new GetEInvoiceByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CheckVknMukellef ═══

    [Fact]
    public async Task CheckVknMukellef_NullRequest_Throws()
    {
        var provider = new Mock<IEInvoiceProvider>();
        var handler = new CheckVknMukellefHandler(provider.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ ApproveInvoice ═══

    [Fact]
    public async Task ApproveInvoice_NullRequest_Throws()
    {
        var repo = new Mock<IInvoiceRepository>();
        var logger = Mock.Of<ILogger<ApproveInvoiceHandler>>();
        var handler = new ApproveInvoiceHandler(repo.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ BulkCreateInvoice ═══

    [Fact]
    public async Task BulkCreateInvoice_NullRequest_Throws()
    {
        var invoiceRepo = new Mock<IInvoiceRepository>();
        var orderRepo = new Mock<IOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = Mock.Of<ILogger<BulkCreateInvoiceHandler>>();
        var handler = new BulkCreateInvoiceHandler(invoiceRepo.Object, orderRepo.Object, uow.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetInvoiceProviders ═══

    [Fact]
    public async Task GetInvoiceProviders_ReturnsProviderList()
    {
        var handler = new GetInvoiceProvidersHandler();
        var result = await handler.Handle(new GetInvoiceProvidersQuery(), CancellationToken.None);
        result.Should().NotBeEmpty();
    }
}
