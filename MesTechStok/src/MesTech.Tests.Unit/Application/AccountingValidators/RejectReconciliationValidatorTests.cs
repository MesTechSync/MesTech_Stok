using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RejectReconciliation;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class RejectReconciliationValidatorTests
{
    private readonly RejectReconciliationValidator _validator = new();

    private static RejectReconciliationCommand ValidCommand() => new(
        MatchId: Guid.NewGuid(),
        ReviewedBy: Guid.NewGuid(),
        Reason: "Tutar uyumsuzlugu"
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

    [Fact]
    public async Task ReasonTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Reason = new string('R', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task NullReason_PassesValidation()
    {
        var cmd = ValidCommand() with { Reason = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
