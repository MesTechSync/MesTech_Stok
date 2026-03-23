using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class EarnPointsCommandValidatorTests
{
    private readonly EarnPointsCommandValidator _sut = new();

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
    public async Task EmptyOrderId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OrderId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public async Task ZeroOrderAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OrderAmount = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderAmount");
    }

    [Fact]
    public async Task NegativeOrderAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { OrderAmount = -100 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderAmount");
    }

    private static EarnPointsCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        CustomerId: Guid.NewGuid(),
        OrderId: Guid.NewGuid(),
        OrderAmount: 250.50m
    );
}
