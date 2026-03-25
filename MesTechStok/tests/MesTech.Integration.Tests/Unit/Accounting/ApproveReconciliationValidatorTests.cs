using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApproveReconciliationValidatorTests
{
    private readonly ApproveReconciliationValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ApproveReconciliationCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MatchId_Fails()
    {
        var cmd = new ApproveReconciliationCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MatchId");
    }
}
