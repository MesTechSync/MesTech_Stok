using FluentAssertions;
using MesTech.Application.Features.Reports.SalesAnalytics;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetSalesAnalyticsValidatorTests
{
    private readonly GetSalesAnalyticsValidator _sut = new();

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

    private static GetSalesAnalyticsQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), From: DateTime.UtcNow.AddMonths(-1), To: DateTime.UtcNow);
}
