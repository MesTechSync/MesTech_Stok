using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// CancelEInvoiceHandler: e-fatura iptal — provider + domain cancel.
/// Kritik: önce provider'da iptal, sonra domain'de cancel.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "InvoiceChain")]
public class CancelEInvoiceHandlerTests
{
    private readonly Mock<IEInvoiceDocumentRepository> _docRepo = new();
    private readonly Mock<IEInvoiceProvider> _provider = new();

    private CancelEInvoiceHandler CreateHandler() =>
        new(_docRepo.Object, _provider.Object);

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsFalse()
    {
        _docRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EInvoiceDocument?)null);

        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), "Test iptal");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
