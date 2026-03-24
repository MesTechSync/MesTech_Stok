using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseSummary;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetIncomeExpenseSummaryValidatorTests
{
    private readonly GetIncomeExpenseSummaryValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetIncomeExpenseSummaryQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetIncomeExpenseSummaryQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
