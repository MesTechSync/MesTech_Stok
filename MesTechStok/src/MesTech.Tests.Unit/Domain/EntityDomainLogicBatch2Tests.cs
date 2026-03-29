using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

// ════════════════════════════════════════════════════════
// DEV5 TUR 7: Entity domain logic batch 2
// FinanceExpense (state machine), DropshippingPoolProduct,
// OnboardingProgress (wizard), DocumentFolder (system guard)
// ════════════════════════════════════════════════════════

#region FinanceExpense — Approval State Machine

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class FinanceExpenseDomainTests
{
    private static FinanceExpense CreateDraft()
        => FinanceExpense.Create(Guid.NewGuid(), "Kargo faturası", 150m,
            ExpenseCategory.Other, DateTime.UtcNow);

    // ── Factory ──

    [Fact]
    public void Create_ValidParams_ReturnsDraftStatus()
    {
        var expense = CreateDraft();
        expense.Title.Should().Be("Kargo faturası");
        expense.Amount.Should().Be(150m);
        expense.Status.Should().Be(ExpenseStatus.Draft);
        expense.Category.Should().Be(ExpenseCategory.Other);
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        var act = () => FinanceExpense.Create(Guid.NewGuid(), "", 100m,
            ExpenseCategory.Other, DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        var act = () => FinanceExpense.Create(Guid.NewGuid(), "Test", 0m,
            ExpenseCategory.Other, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        var act = () => FinanceExpense.Create(Guid.NewGuid(), "Test", -50m,
            ExpenseCategory.Other, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── State transitions ──

    [Fact]
    public void Submit_FromDraft_ChangesToSubmitted()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Status.Should().Be(ExpenseStatus.Submitted);
    }

    [Fact]
    public void Submit_FromSubmitted_Throws()
    {
        var expense = CreateDraft();
        expense.Submit();
        var act = () => expense.Submit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_FromSubmitted_ChangesToApproved()
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
    public void Approve_FromDraft_Throws()
    {
        var expense = CreateDraft();
        var act = () => expense.Approve(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_RaisesDomainEvent()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Approve(Guid.NewGuid());
        expense.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Reject_FromSubmitted_ChangesToRejected()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Reject("Fatura eksik");

        expense.Status.Should().Be(ExpenseStatus.Rejected);
        expense.Notes.Should().Contain("Red: Fatura eksik");
    }

    [Fact]
    public void Reject_FromDraft_Throws()
    {
        var expense = CreateDraft();
        var act = () => expense.Reject("reason");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsPaid_FromApproved_ChangesToPaid()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Approve(Guid.NewGuid());
        expense.MarkAsPaid(Guid.NewGuid());

        expense.Status.Should().Be(ExpenseStatus.Paid);
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
    public void MarkAsPaid_RaisesDomainEvent()
    {
        var expense = CreateDraft();
        expense.Submit();
        expense.Approve(Guid.NewGuid());
        expense.ClearDomainEvents();
        expense.MarkAsPaid(Guid.NewGuid());
        expense.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void AttachDocument_SetsDocumentId()
    {
        var expense = CreateDraft();
        var docId = Guid.NewGuid();
        expense.AttachDocument(docId);
        expense.DocumentId.Should().Be(docId);
    }
}

#endregion

#region DropshippingPoolProduct

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class DropshippingPoolProductDomainTests
{
    // ── Constructor ──

    [Fact]
    public void Constructor_ValidParams_Succeeds()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 99.90m);
        product.PoolPrice.Should().Be(99.90m);
        product.IsActive.Should().BeTrue();
        product.ReliabilityScore.Should().Be(0);
    }

    [Fact]
    public void Constructor_EmptyPoolId_Throws()
    {
        var act = () => new DropshippingPoolProduct(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 50m);
        act.Should().Throw<ArgumentException>().WithMessage("*PoolId*");
    }

    [Fact]
    public void Constructor_EmptyProductId_Throws()
    {
        var act = () => new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 50m);
        act.Should().Throw<ArgumentException>().WithMessage("*ProductId*");
    }

    [Fact]
    public void Constructor_NegativePrice_Throws()
    {
        var act = () => new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -10m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── UpdatePrice ──

    [Fact]
    public void UpdatePrice_ValidPrice_Updates()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        product.UpdatePrice(75m);
        product.PoolPrice.Should().Be(75m);
    }

    [Fact]
    public void UpdatePrice_NegativePrice_Throws()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        var act = () => product.UpdatePrice(-5m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdatePrice_ZeroPrice_Succeeds()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        product.UpdatePrice(0m);
        product.PoolPrice.Should().Be(0m);
    }

    // ── Activate / Deactivate ──

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        product.Deactivate();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsActive()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        product.Deactivate();
        product.Activate();
        product.IsActive.Should().BeTrue();
    }

    // ── UpdateReliability ──

    [Fact]
    public void UpdateReliability_ValidScore_Updates()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        product.UpdateReliability(85.5m, 2);
        product.ReliabilityScore.Should().Be(85.5m);
        product.ReliabilityColor.Should().Be(2);
    }

    [Fact]
    public void UpdateReliability_ScoreAbove100_Throws()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        var act = () => product.UpdateReliability(101m, 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateReliability_NegativeScore_Throws()
    {
        var product = new DropshippingPoolProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50m);
        var act = () => product.UpdateReliability(-1m, 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

#endregion

#region OnboardingProgress — Wizard State Machine

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class OnboardingProgressDomainTests
{
    [Fact]
    public void Start_ReturnsRegistrationStep()
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        progress.CurrentStep.Should().Be(OnboardingStep.Registration);
        progress.IsCompleted.Should().BeFalse();
        progress.CompletionPercentage.Should().Be(14); // 1*100/7
    }

    [Fact]
    public void CompleteCurrentStep_AdvancesToNext()
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        progress.CompleteCurrentStep(); // Registration → CompanyInfo
        progress.CurrentStep.Should().Be(OnboardingStep.CompanyInfo);
    }

    [Fact]
    public void CompleteAllSteps_MarksCompleted()
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        for (int i = 0; i < 7; i++)
            progress.CompleteCurrentStep();

        progress.IsCompleted.Should().BeTrue();
        progress.CompletedAt.Should().NotBeNull();
        progress.CompletionPercentage.Should().Be(100);
        progress.DomainEvents.Should().ContainSingle(); // OnboardingCompletedEvent
    }

    [Fact]
    public void CompleteCurrentStep_WhenAlreadyCompleted_Throws()
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        for (int i = 0; i < 7; i++)
            progress.CompleteCurrentStep();

        var act = () => progress.CompleteCurrentStep();
        act.Should().Throw<InvalidOperationException>().WithMessage("*tamamlandi*");
    }

    [Fact]
    public void SkipToStep_SetsCurrentStep()
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        progress.SkipToStep(OnboardingStep.InitialSync);
        progress.CurrentStep.Should().Be(OnboardingStep.InitialSync);
    }

    [Fact]
    public void SkipToStep_WhenCompleted_Throws()
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        for (int i = 0; i < 7; i++)
            progress.CompleteCurrentStep();

        var act = () => progress.SkipToStep(OnboardingStep.Registration);
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(OnboardingStep.Registration, 14)]
    [InlineData(OnboardingStep.CompanyInfo, 28)]
    [InlineData(OnboardingStep.FirstStore, 42)]
    [InlineData(OnboardingStep.Dashboard, 100)]
    public void CompletionPercentage_CalculatesCorrectly(OnboardingStep step, int expected)
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        progress.SkipToStep(step);
        progress.CompletionPercentage.Should().Be(expected);
    }
}

#endregion

#region DocumentFolder — System Guard

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class DocumentFolderDomainTests
{
    [Fact]
    public void Create_ValidParams_Succeeds()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Faturalar");
        folder.Name.Should().Be("Faturalar");
        folder.IsSystem.Should().BeFalse();
        folder.ParentFolderId.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => DocumentFolder.Create(Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_SystemFolder_SetsFlag()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Sistem", isSystem: true);
        folder.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void Rename_NormalFolder_Succeeds()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Eski Ad");
        folder.Rename("Yeni Ad");
        folder.Name.Should().Be("Yeni Ad");
    }

    [Fact]
    public void Rename_SystemFolder_Throws()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Sistem", isSystem: true);
        var act = () => folder.Rename("Yeni");
        act.Should().Throw<InvalidOperationException>().WithMessage("*System*");
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Test");
        var act = () => folder.Rename("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Delete_NormalFolder_SetsIsDeleted()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Temp");
        folder.Delete();
        folder.IsDeleted.Should().BeTrue();
        folder.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_SystemFolder_Throws()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Sistem", isSystem: true);
        var act = () => folder.Delete();
        act.Should().Throw<InvalidOperationException>().WithMessage("*System*");
    }
}

#endregion
