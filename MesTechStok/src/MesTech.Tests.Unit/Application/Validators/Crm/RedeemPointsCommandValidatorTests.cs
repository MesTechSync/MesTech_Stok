using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class RedeemPointsCommandValidatorTests
{
    private readonly RedeemPointsCommandValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyCustomerId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public async Task ZeroPoints_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PointsToRedeem = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PointsToRedeem");
    }

    [Fact]
    public async Task NegativePoints_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PointsToRedeem = -5 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PointsToRedeem");
    }

    private static RedeemPointsCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CustomerId: Guid.NewGuid(),
        PointsToRedeem: 100
    );
}
