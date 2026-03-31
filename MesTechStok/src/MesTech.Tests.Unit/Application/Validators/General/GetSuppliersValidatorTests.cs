using FluentAssertions;
using MesTech.Application.Queries.GetSuppliers;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetSuppliersValidatorTests
{
    private readonly GetSuppliersValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetSuppliersQuery CreateValidQuery() => new();
}
