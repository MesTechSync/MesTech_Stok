using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UpdateFixedExpenseHandlerTests
{
    private readonly Mock<IFixedExpenseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateFixedExpenseHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UpdateFixedExpenseHandlerTests()
    {
        _sut = new UpdateFixedExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_UpdateAmount_ShouldSucceed()
    {
        // Arrange
        var expense = FixedExpense.Create(TenantId, "Internet", 500m, 15, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);
        var command = new UpdateFixedExpenseCommand(expense.Id, MonthlyAmount: 750m);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(expense, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeactivateExpense_ShouldSucceed()
    {
        // Arrange
        var expense = FixedExpense.Create(TenantId, "Telefon", 200m, 10, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);
        var command = new UpdateFixedExpenseCommand(expense.Id, IsActive: false);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(expense, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedExpense?)null);
        var command = new UpdateFixedExpenseCommand(Guid.NewGuid(), MonthlyAmount: 100m);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
