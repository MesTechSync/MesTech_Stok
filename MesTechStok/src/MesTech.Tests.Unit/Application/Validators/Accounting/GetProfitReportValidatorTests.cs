using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetProfitReportValidatorTests
{
    private readonly GetProfitReportValidator _sut = new();

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

    private static GetProfitReportQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), Period: "2026-03", Platform: "test");
}
