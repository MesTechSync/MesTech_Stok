using FluentAssertions;
using MesTech.Application.Queries.SearchProductsForImageMatch;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

[Trait("Category", "Unit")]
public class SearchProductsForImageMatchValidatorTests
{
    private readonly SearchProductsForImageMatchValidator _sut = new();

    private static SearchProductsForImageMatchQuery CreateValidQuery() => new();

    [Fact]
    public async Task ValidQuery_ShouldPassValidation()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NewInstance_ShouldPassValidation()
    {
        var query = new SearchProductsForImageMatchQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidQuery_ShouldHaveNoErrors()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.Errors.Should().BeEmpty();
    }
}
