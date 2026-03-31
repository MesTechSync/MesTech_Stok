using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetOrderDetail;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Orders;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetOrderDetailValidatorTests
{
    private readonly GetOrderDetailValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var input = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyOrderId_ShouldFail()
    {
        var input = CreateValidQuery() with { OrderId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    private static GetOrderDetailQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), OrderId: Guid.NewGuid());
}
