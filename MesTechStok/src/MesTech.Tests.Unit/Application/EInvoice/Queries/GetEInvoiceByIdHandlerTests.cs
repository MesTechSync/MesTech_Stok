using FluentAssertions;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.EInvoice.Queries;

[Trait("Category", "Unit")]
public class GetEInvoiceByIdHandlerTests
{
    private readonly Mock<IEInvoiceDocumentRepository> _repository = new();

    private GetEInvoiceByIdHandler CreateHandler() =>
        new(_repository.Object);

    [Fact]
    public async Task Handle_ExistingDocument_ShouldReturnMappedDto()
    {
        // Arrange
        var gibUuid = Guid.NewGuid().ToString();
        var doc = EInvoiceDocument.Create(
            gibUuid, "ETT-2026-001",
            EInvoiceScenario.TICARIFATURA, EInvoiceType.SATIS,
            DateTime.UtcNow, "1234567890", "MesTech A.S.",
            "Alici Ltd.", "Sovos", "system");

        _repository
            .Setup(r => r.GetByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var handler = CreateHandler();
        var query = new GetEInvoiceByIdQuery(doc.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.EttnNo.Should().Be("ETT-2026-001");
        result.SellerVkn.Should().Be("1234567890");
        result.BuyerTitle.Should().Be("Alici Ltd.");
        result.ProviderId.Should().Be("Sovos");
    }

    [Fact]
    public async Task Handle_NonExistentId_ShouldReturnNull()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EInvoiceDocument?)null);

        var handler = CreateHandler();
        var query = new GetEInvoiceByIdQuery(missingId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
