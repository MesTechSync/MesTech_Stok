using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Billing;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ChangeSubscriptionPlanValidatorTests
{
    private readonly ChangeSubscriptionPlanValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ChangeSubscriptionPlanCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new ChangeSubscriptionPlanCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyNewPlanId_ShouldFail()
    {
        var cmd = new ChangeSubscriptionPlanCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPlanId");
    }

    [Fact]
    public async Task BothEmpty_ShouldFailWithTwoErrors()
    {
        var cmd = new ChangeSubscriptionPlanCommand(Guid.Empty, Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
