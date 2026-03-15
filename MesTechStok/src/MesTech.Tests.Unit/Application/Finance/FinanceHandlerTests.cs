using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Finance;

[Trait("Category", "Unit")]
public class FinanceHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _approver = Guid.NewGuid();
    private static readonly Guid _bankId = Guid.NewGuid();

    private static FinanceExpense MakeApprovedExpense()
    {
        var e = FinanceExpense.Create(_tenantId, "Test Expense", 500m, ExpenseCategory.Software, DateTime.Today);
        e.Submit();
        e.Approve(_approver);
        return e;
    }

    [Fact]
    public async Task ApproveExpenseHandler_SubmittedExpense_ShouldApprove()
    {
        var expense = FinanceExpense.Create(_tenantId, "Test Expense", 500m, ExpenseCategory.Other, DateTime.Today);
        expense.Submit();
        var mockRepo = new Mock<IFinanceExpenseRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        await new ApproveExpenseHandler(mockRepo.Object, Mock.Of<IUnitOfWork>())
            .Handle(new ApproveExpenseCommand(expense.Id, _approver), CancellationToken.None);

        expense.Status.Should().Be(ExpenseStatus.Approved);
    }

    [Fact]
    public async Task MarkExpensePaidHandler_ApprovedExpense_ShouldMarkPaid()
    {
        var expense = MakeApprovedExpense();
        var mockRepo = new Mock<IFinanceExpenseRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        await new MarkExpensePaidHandler(mockRepo.Object, Mock.Of<IUnitOfWork>())
            .Handle(new MarkExpensePaidCommand(expense.Id, _bankId), CancellationToken.None);

        expense.Status.Should().Be(ExpenseStatus.Paid);
    }

    [Fact]
    public async Task ApproveExpenseHandler_NotFound_ShouldThrow()
    {
        var mockRepo = new Mock<IFinanceExpenseRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FinanceExpense?)null);

        var act = () => new ApproveExpenseHandler(mockRepo.Object, Mock.Of<IUnitOfWork>())
            .Handle(new ApproveExpenseCommand(Guid.NewGuid(), _approver), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
