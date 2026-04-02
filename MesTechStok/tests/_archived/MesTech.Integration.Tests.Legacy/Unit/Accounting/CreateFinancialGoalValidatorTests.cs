using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateFinancialGoalValidatorTests
{
    private readonly CreateFinancialGoalValidator _validator = new();

    private static CreateFinancialGoalCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Q2 Gelir Hedefi",
        TargetAmount: 500000m,
        StartDate: new DateTime(2026, 4, 1),
        EndDate: new DateTime(2026, 6, 30));

    [Fact]
    public void Valid_Command_Passes()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Title_Fails()
    {
        var cmd = ValidCommand() with { Title = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Title_Over500_Fails()
    {
        var cmd = ValidCommand() with { Title = new string('T', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_TargetAmount_Fails()
    {
        var cmd = ValidCommand() with { TargetAmount = -1m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_TargetAmount_Passes()
    {
        var cmd = ValidCommand() with { TargetAmount = 0m };
        _validator.Validate(cmd).Errors.Should().NotContain(e => e.PropertyName == "TargetAmount");
    }
}
