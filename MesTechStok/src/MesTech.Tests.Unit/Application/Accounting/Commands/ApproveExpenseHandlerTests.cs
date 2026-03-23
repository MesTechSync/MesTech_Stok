using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class ApproveExpenseHandlerTests
{
    private readonly Mock<IFinanceExpenseRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ApproveExpenseHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public ApproveExpenseHandlerTests()
    {
        _sut = new ApproveExpenseHandler(_repoMock.Object, _uowMock.Object);
    }

    private static FinanceExpense CreateSubmittedExpense()
    {
        var expense = FinanceExpense.Create(
            TenantId, "Kargo Masrafi", 500m, ExpenseCategory.Other,
            new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));
        expense.Submit();
        return expense;
    }

    [Fact]
    public async Task Handle_SubmittedExpense_ApprovesSuccessfully()
    {
        // Arrange
        var expense = CreateSubmittedExpense();
        var approverId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);

        var command = new ApproveExpenseCommand(expense.Id, approverId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        expense.Status.Should().Be(ExpenseStatus.Approved);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentExpense_ThrowsInvalidOperationException()
    {
        // Arrange
        var expenseId = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByIdAsync(expenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceExpense?)null);

        var command = new ApproveExpenseCommand(expenseId, Guid.NewGuid());

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{expenseId}*not found*");
    }

    [Fact]
    public async Task Handle_DraftExpense_ThrowsInvalidOperationException()
    {
        // Arrange — expense in Draft status, not Submitted
        var expense = FinanceExpense.Create(
            TenantId, "Test Expense", 100m, ExpenseCategory.Other,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        _repoMock
            .Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);

        var command = new ApproveExpenseCommand(expense.Id, Guid.NewGuid());

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert — domain should reject approving a non-submitted expense
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
