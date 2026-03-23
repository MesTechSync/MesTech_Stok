using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Domain.Entities.Billing;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Billing;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateSubscriptionValidatorTests
{
    private readonly CreateSubscriptionValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new CreateSubscriptionCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyPlanId_ShouldFail()
    {
        var cmd = new CreateSubscriptionCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlanId");
    }

    [Fact]
    public async Task InvalidPeriodEnum_ShouldFail()
    {
        var cmd = new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), (BillingPeriod)99);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Period");
    }

    [Theory]
    [InlineData(BillingPeriod.Monthly)]
    [InlineData(BillingPeriod.Annual)]
    public async Task ValidPeriod_ShouldPass(BillingPeriod period)
    {
        var cmd = new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), period);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
