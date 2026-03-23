using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Billing;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CancelSubscriptionValidatorTests
{
    private readonly CancelSubscriptionValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), "Plani degistiriyorum");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new CancelSubscriptionCommand(Guid.Empty, Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptySubscriptionId_ShouldFail()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubscriptionId");
    }

    [Fact]
    public async Task ReasonExceeds500Chars_ShouldFail()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), new string('R', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task ReasonNull_ShouldPass()
    {
        var cmd = new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
