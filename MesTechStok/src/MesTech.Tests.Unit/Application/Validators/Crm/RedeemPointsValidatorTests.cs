using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Crm")]
public class RedeemPointsValidatorTests
{
    private readonly RedeemPointsValidator _validator = new();

    private static RedeemPointsCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CustomerId: Guid.NewGuid(),
        PointsToRedeem: 100);

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyCustomerId_Fails()
    {
        var cmd = ValidCommand() with { CustomerId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroPoints_Fails()
    {
        var cmd = ValidCommand() with { PointsToRedeem = 0 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativePoints_Fails()
    {
        var cmd = ValidCommand() with { PointsToRedeem = -10 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
