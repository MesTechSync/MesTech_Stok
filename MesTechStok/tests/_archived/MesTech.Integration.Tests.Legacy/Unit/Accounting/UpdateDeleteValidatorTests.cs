using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.RunReconciliation;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateDeleteValidatorTests
{
    // ═══ UpdateChartOfAccount ═══

    [Fact]
    public void UpdateChartOfAccount_Valid_Passes()
    {
        var validator = new UpdateChartOfAccountValidator();
        var cmd = new UpdateChartOfAccountCommand(Guid.NewGuid(), "Kasa Hesabı");
        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateChartOfAccount_EmptyId_Fails()
    {
        var validator = new UpdateChartOfAccountValidator();
        var cmd = new UpdateChartOfAccountCommand(Guid.Empty, "Kasa");
        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateChartOfAccount_EmptyName_Fails()
    {
        var validator = new UpdateChartOfAccountValidator();
        var cmd = new UpdateChartOfAccountCommand(Guid.NewGuid(), "");
        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateChartOfAccount_Name_Over200_Fails()
    {
        var validator = new UpdateChartOfAccountValidator();
        var cmd = new UpdateChartOfAccountCommand(Guid.NewGuid(), new string('K', 201));
        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ DeleteChartOfAccount ═══

    [Fact]
    public void DeleteChartOfAccount_Valid_Passes()
    {
        var validator = new DeleteChartOfAccountValidator();
        var cmd = new DeleteChartOfAccountCommand(Guid.NewGuid());
        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteChartOfAccount_EmptyId_Fails()
    {
        var validator = new DeleteChartOfAccountValidator();
        var cmd = new DeleteChartOfAccountCommand(Guid.Empty);
        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ UpdateFixedExpense ═══

    [Fact]
    public void UpdateFixedExpense_Valid_Passes()
    {
        var validator = new UpdateFixedExpenseValidator();
        var cmd = new UpdateFixedExpenseCommand(Guid.NewGuid(), MonthlyAmount: 30000m);
        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateFixedExpense_EmptyId_Fails()
    {
        var validator = new UpdateFixedExpenseValidator();
        var cmd = new UpdateFixedExpenseCommand(Guid.Empty);
        validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ RunReconciliation ═══

    [Fact]
    public void RunReconciliation_Valid_Passes()
    {
        var validator = new RunReconciliationValidator();
        var cmd = new RunReconciliationCommand(Guid.NewGuid());
        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RunReconciliation_EmptyTenantId_Fails()
    {
        var validator = new RunReconciliationValidator();
        var cmd = new RunReconciliationCommand(Guid.Empty);
        validator.Validate(cmd).IsValid.Should().BeFalse();
    }
}
