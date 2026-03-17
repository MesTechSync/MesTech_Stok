using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// RecordCargoExpenseHandler tests — cargo expense creation and persistence.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class RecordCargoExpenseHandlerTests
{
    private readonly Mock<ICargoExpenseRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly RecordCargoExpenseHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public RecordCargoExpenseHandlerTests()
    {
        _repoMock = new Mock<ICargoExpenseRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _sut = new RecordCargoExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNonEmptyGuidAndCallsAddAsync()
    {
        // Arrange
        var command = new RecordCargoExpenseCommand(
            TenantId: _tenantId,
            CarrierName: "Yurtici Kargo",
            Cost: 45.50m,
            OrderId: "TY-123456",
            TrackingNumber: "TRK-001");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(
                It.Is<CargoExpense>(e =>
                    e.CarrierName == "Yurtici Kargo" &&
                    e.Cost == 45.50m &&
                    e.TenantId == _tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_EmptyCarrierName_ThrowsArgumentException()
    {
        // Arrange
        var command = new RecordCargoExpenseCommand(
            TenantId: _tenantId,
            CarrierName: "",
            Cost: 25m);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
}
