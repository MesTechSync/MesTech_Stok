using FluentAssertions;
using MesTech.Application.Queries.GetBitrix24DealStatus;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetBitrix24DealStatusValidatorTests
{
    private readonly GetBitrix24DealStatusValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyOrderId_ShouldFail()
    {
        var input = CreateValidQuery() with { OrderId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    private static GetBitrix24DealStatusQuery CreateValidQuery() => new(OrderId: Guid.NewGuid());
}
