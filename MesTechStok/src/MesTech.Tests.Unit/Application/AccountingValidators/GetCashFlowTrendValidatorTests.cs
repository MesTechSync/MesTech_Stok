using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCashFlowTrend;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetCashFlowTrendValidatorTests
{
    private readonly GetCashFlowTrendValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetCashFlowTrendQuery(TenantId: Guid.NewGuid(), Months: 6);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetCashFlowTrendQuery(TenantId: Guid.Empty, Months: 6);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task MonthsZero_Fails()
    {
        var query = new GetCashFlowTrendQuery(TenantId: Guid.NewGuid(), Months: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task MonthsOver24_Fails()
    {
        var query = new GetCashFlowTrendQuery(TenantId: Guid.NewGuid(), Months: 25);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
