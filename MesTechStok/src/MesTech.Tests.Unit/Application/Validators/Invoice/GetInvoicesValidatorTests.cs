using FluentAssertions;
using MesTech.Application.Features.Invoice.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Invoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetInvoicesValidatorTests
{
    private readonly GetInvoicesValidator _sut = new();

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

    private static GetInvoicesQuery CreateValidQuery() => new(Type: null, Status: null, Platform: null, From: null, To: null, Search: "test", Page: 1, PageSize: 20);
}
