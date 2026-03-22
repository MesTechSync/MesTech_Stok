using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class ApproveReconciliationValidatorTests
{
    private readonly ApproveReconciliationValidator _validator = new();

    private static ApproveReconciliationCommand ValidCommand() => new(
        MatchId: Guid.NewGuid(),
        ReviewedBy: Guid.NewGuid()
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyMatchId_FailsValidation()
    {
        var cmd = ValidCommand() with { MatchId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MatchId");
    }
}
