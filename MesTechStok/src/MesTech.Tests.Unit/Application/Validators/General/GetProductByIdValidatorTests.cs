using FluentAssertions;
using MesTech.Application.Queries.GetProductById;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetProductByIdValidatorTests
{
    private readonly GetProductByIdValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyProductId_ShouldFail()
    {
        var input = CreateValidQuery() with { ProductId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    private static GetProductByIdQuery CreateValidQuery() => new(ProductId: Guid.NewGuid());
}
