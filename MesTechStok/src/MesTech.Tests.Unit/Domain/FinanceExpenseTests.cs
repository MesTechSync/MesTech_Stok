using FluentAssertions;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class FinanceExpenseTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private FinanceExpense CreateDraft(decimal amount = 100m) =>
        FinanceExpense.Create(_tenantId, "Test gider", amount, ExpenseCategory.Other, DateTime.UtcNow);

    // ═══ Create Validation ═══

    [Fact]
    public void Create_ValidInput_ReturnsDraftStatus()
    {
        var expense = CreateDraft(500m);
        expense.Status.Should().Be(ExpenseStatus.Draft);
        expense.Amount.Should().Be(500m);
        expense.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        var act = () => FinanceExpense.Create(_tenantId, "Test", 0m, ExpenseCategory.Other, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("amount");
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        var act = () => FinanceExpense.Create(_tenantId, "Test", -50m, ExpenseCategory.Other, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        var act = () => FinanceExpense.Create(_tenantId, "", 100m, ExpenseCategory.Other, DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    // ═══ State Machine: Happy Path ═══

    [Fact]
    public void Submit_FromDraft_SetsSubmittedStatus()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Status.Should().Be(ExpenseStatus.Submitted);
    }

    [Fact]
    public void Approve_FromSubmitted_SetsApprovedWithApprover()
    {
        var expense = CreateDraft();
        expense.Submit();
        var approverId = Guid.NewGuid();
        expense.Approve(approverId);

        expense.Status.Should().Be(ExpenseStatus.Approved);
        expense.ApprovedByUserId.Should().Be(approverId);
        expense.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsPaid_FromApproved_SetsPaidStatus()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Approve(Guid.NewGuid());
        expense.MarkAsPaid(Guid.NewGuid());

        expense.Status.Should().Be(ExpenseStatus.Paid);
    }

    [Fact]
    public void Reject_FromSubmitted_SetsRejectedWithReason()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Reject("Bütçe aşımı");

        expense.Status.Should().Be(ExpenseStatus.Rejected);
        expense.Notes.Should().Contain("Bütçe aşımı");
    }

    // ═══ State Machine: Invalid Transitions ═══

    [Fact]
    public void Submit_FromSubmitted_Throws()
    {
        var expense = CreateDraft();
        expense.Submit();
        var act = () => expense.Submit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_FromDraft_Throws()
    {
        var expense = CreateDraft();
        var act = () => expense.Approve(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_FromDraft_Throws()
    {
        var expense = CreateDraft();
        var act = () => expense.Reject("reason");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsPaid_FromDraft_Throws()
    {
        var expense = CreateDraft();
        var act = () => expense.MarkAsPaid(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsPaid_FromSubmitted_Throws()
    {
        var expense = CreateDraft();
        expense.Submit();
        var act = () => expense.MarkAsPaid(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_FromRejected_Throws()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Reject("no");
        var act = () => expense.Approve(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    // ═══ AttachDocument ═══

    [Fact]
    public void AttachDocument_SetsDocumentId()
    {
        var expense = CreateDraft();
        var docId = Guid.NewGuid();
        expense.AttachDocument(docId);
        expense.DocumentId.Should().Be(docId);
    }
}
