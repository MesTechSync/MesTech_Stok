using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Billing.Commands;

[Trait("Category", "Unit")]
public class CreateBillingInvoiceHandlerTests
{
    private readonly Mock<IBillingInvoiceRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateBillingInvoiceHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SubscriptionId = Guid.NewGuid();

    public CreateBillingInvoiceHandlerTests()
    {
        _sut = new CreateBillingInvoiceHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        _repoMock.Setup(r => r.GetNextSequenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var command = new CreateBillingInvoiceCommand(TenantId, SubscriptionId, 799m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<BillingInvoice>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomTaxRateAndDueDays_ShouldCreateSuccessfully()
    {
        // Arrange
        _repoMock.Setup(r => r.GetNextSequenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        var command = new CreateBillingInvoiceCommand(
            TenantId, SubscriptionId, 1999m, "USD", TaxRate: 0.10m, DueDays: 30);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
