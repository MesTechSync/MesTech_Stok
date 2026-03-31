using FluentAssertions;
using MesTech.Application.Queries.ListQuotations;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Quotations;

[Trait("Category", "Unit")]
public class ListQuotationsValidatorTests
{
    private readonly ListQuotationsValidator _sut = new();

    private static ListQuotationsQuery CreateValidQuery() => new();

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
        var query = new ListQuotationsQuery();
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
