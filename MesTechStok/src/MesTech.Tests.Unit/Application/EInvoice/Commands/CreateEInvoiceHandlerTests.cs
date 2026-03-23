using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.EInvoice.Commands;

[Trait("Category", "Unit")]
public class CreateEInvoiceHandlerTests
{
    private readonly Mock<IEInvoiceDocumentRepository> _repoMock = new();
    private readonly CreateEInvoiceHandler _sut;

    public CreateEInvoiceHandlerTests()
    {
        _sut = new CreateEInvoiceHandler(_repoMock.Object);
    }

    private static CreateEInvoiceCommand ValidCommand() => new(
        OrderId: Guid.NewGuid(),
        BuyerVkn: "1234567890",
        BuyerTitle: "Test Alici Ltd.",
        BuyerEmail: "alici@test.com",
        Scenario: EInvoiceScenario.TICARIFATURA,
        Type: EInvoiceType.SATIS,
        IssueDate: DateTime.UtcNow,
        CurrencyCode: "TRY",
        Lines: new List<CreateEInvoiceLineRequest>
        {
            new("Urun A", Quantity: 2, UnitCode: "C62", UnitPrice: 100m, TaxPercent: 20, AllowanceAmount: 0m, ProductId: null)
        },
        ProviderId: "sovos");

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = ValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleLines_ShouldCreateDocWithAllLines()
    {
        // Arrange
        EInvoiceDocument? capturedDoc = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()))
            .Callback<EInvoiceDocument, CancellationToken>((doc, _) => capturedDoc = doc);

        var command = ValidCommand() with
        {
            Lines = new List<CreateEInvoiceLineRequest>
            {
                new("Urun A", 1, "C62", 100m, 20, 0m, null),
                new("Urun B", 3, "C62", 50m, 18, 10m, null),
                new("Hizmet C", 1, "HUR", 200m, 20, 0m, null)
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedDoc.Should().NotBeNull();
        capturedDoc!.Lines.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_SingleLine_ShouldSetFinancialsCorrectly()
    {
        // Arrange
        EInvoiceDocument? capturedDoc = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<EInvoiceDocument>(), It.IsAny<CancellationToken>()))
            .Callback<EInvoiceDocument, CancellationToken>((doc, _) => capturedDoc = doc);

        var command = ValidCommand() with
        {
            Lines = new List<CreateEInvoiceLineRequest>
            {
                new("Urun A", Quantity: 1, UnitCode: "C62", UnitPrice: 100m, TaxPercent: 20, AllowanceAmount: 0m, ProductId: null)
            }
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedDoc.Should().NotBeNull();
        capturedDoc!.PayableAmount.Should().BeGreaterThan(0);
        capturedDoc.CurrencyCode.Should().Be("TRY");
    }
}
