using FluentAssertions;
using MesTech.Application.Commands.CancelOrder;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CancelOrderValidatorTests
{
    private readonly CancelOrderValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidCommand();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyOrderId_ShouldFail()
    {
        var input = CreateValidCommand() with { OrderId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    private static CancelOrderCommand CreateValidCommand() => new(OrderId: Guid.NewGuid());
}
