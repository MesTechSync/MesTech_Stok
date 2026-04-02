using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class FinanceExpenseTests
{
    [Fact]
    public void Create_ValidInput_ShouldSetDraftStatus()
    {
        var expense = FinanceExpense.Create(Guid.NewGuid(), "Ofis malzemesi", 250m, ExpenseCategory.Other, DateTime.UtcNow);
        expense.Amount.Should().Be(250m);
        expense.Status.Should().Be(ExpenseStatus.Draft);
    }

    [Fact]
    public void Create_ZeroAmount_ShouldThrow()
    {
        var act = () => FinanceExpense.Create(Guid.NewGuid(), "Test", 0m, ExpenseCategory.Other, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Submit_FromDraft_ShouldChangeStatus()
    {
        var expense = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Other, DateTime.UtcNow);
        expense.Submit();
        expense.Status.Should().Be(ExpenseStatus.Submitted);
    }

    [Fact]
    public void Approve_FromSubmitted_ShouldSetApprover()
    {
        var approverId = Guid.NewGuid();
        var expense = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Other, DateTime.UtcNow);
        expense.Submit();
        expense.Approve(approverId);
        expense.Status.Should().Be(ExpenseStatus.Approved);
        expense.ApprovedByUserId.Should().Be(approverId);
    }

    [Fact]
    public void Approve_FromDraft_ShouldThrow()
    {
        var expense = FinanceExpense.Create(Guid.NewGuid(), "Test", 100m, ExpenseCategory.Other, DateTime.UtcNow);
        var act = () => expense.Approve(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }
}

// FulfillmentShipment — constructor parametreli, Create factory gerekli
