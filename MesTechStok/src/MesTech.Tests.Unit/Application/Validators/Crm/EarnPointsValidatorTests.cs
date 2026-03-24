using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.EarnPoints;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Crm")]
public class EarnPointsValidatorTests
{
    private readonly EarnPointsValidator _validator = new();

    private static EarnPointsCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CustomerId: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        OrderAmount: 250m);

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
    public async Task EmptyOrderId_Fails()
    {
        var cmd = ValidCommand() with { OrderId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ZeroOrderAmount_Fails()
    {
        var cmd = ValidCommand() with { OrderAmount = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NegativeOrderAmount_Fails()
    {
        var cmd = ValidCommand() with { OrderAmount = -50m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
