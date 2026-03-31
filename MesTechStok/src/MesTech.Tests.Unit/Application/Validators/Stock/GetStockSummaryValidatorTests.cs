using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stock;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetStockSummaryValidatorTests
{
    private readonly GetStockSummaryValidator _sut = new();

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

    private static GetStockSummaryQuery CreateValidQuery() => new(TenantId: Guid.NewGuid());
}
