using FluentAssertions;
using MesTech.Application.Queries.GetCustomersPaged;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetCustomersPagedValidatorTests
{
    private readonly GetCustomersPagedValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Page_WhenZero_ShouldFail()
    {
        var input = CreateValidQuery() with { Page = 0 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSize_WhenZero_ShouldFail()
    {
        var input = CreateValidQuery() with { PageSize = 0 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
    }

    private static GetCustomersPagedQuery CreateValidQuery() => new(Page: 1, PageSize: 20, SearchTerm: "test");
}
