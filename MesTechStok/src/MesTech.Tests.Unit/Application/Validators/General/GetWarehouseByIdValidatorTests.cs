using FluentAssertions;
using MesTech.Application.Queries.GetWarehouseById;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetWarehouseByIdValidatorTests
{
    private readonly GetWarehouseByIdValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyWarehouseId_ShouldFail()
    {
        var input = CreateValidQuery() with { WarehouseId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WarehouseId");
    }

    private static GetWarehouseByIdQuery CreateValidQuery() => new(WarehouseId: Guid.NewGuid());
}
