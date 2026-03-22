using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class DeleteFixedExpenseHandlerTests
{
    private readonly Mock<IFixedExpenseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteFixedExpenseHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public DeleteFixedExpenseHandlerTests()
    {
        _sut = new DeleteFixedExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingExpense_ShouldSoftDelete()
    {
        // Arrange
        var expense = FixedExpense.Create(TenantId, "Internet", 500m, 15, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);
        var command = new DeleteFixedExpenseCommand(expense.Id);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        expense.IsDeleted.Should().BeTrue();
        expense.DeletedAt.Should().NotBeNull();
        _repoMock.Verify(r => r.UpdateAsync(expense, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedExpense?)null);

        // Act
        var act = () => _sut.Handle(new DeleteFixedExpenseCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldSetDeletedAtToUtcNow()
    {
        // Arrange
        var expense = FixedExpense.Create(TenantId, "Telefon", 200m, 10, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);
        var before = DateTime.UtcNow;

        // Act
        await _sut.Handle(new DeleteFixedExpenseCommand(expense.Id), CancellationToken.None);

        // Assert
        expense.DeletedAt.Should().BeOnOrAfter(before);
        expense.DeletedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}
