using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateFinancialGoalValidatorTests
{
    private readonly CreateFinancialGoalValidator _validator = new();

    private static CreateFinancialGoalCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Yillik Ciro Hedefi",
        TargetAmount: 1_000_000m,
        StartDate: new DateTime(2026, 1, 1),
        EndDate: new DateTime(2026, 12, 31)
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyTitle_FailsValidation()
    {
        var cmd = ValidCommand() with { Title = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task TitleTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Title = new string('T', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task NegativeTargetAmount_FailsValidation()
    {
        var cmd = ValidCommand() with { TargetAmount = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetAmount");
    }

    [Fact]
    public async Task ZeroTargetAmount_PassesValidation()
    {
        var cmd = ValidCommand() with { TargetAmount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
