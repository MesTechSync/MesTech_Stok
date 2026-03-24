using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetIncomeExpenseList;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetIncomeExpenseListValidatorTests
{
    private readonly GetIncomeExpenseListValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetIncomeExpenseListQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetIncomeExpenseListQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageZero_Fails()
    {
        var query = new GetIncomeExpenseListQuery(TenantId: Guid.NewGuid(), Page: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSizeZero_Fails()
    {
        var query = new GetIncomeExpenseListQuery(TenantId: Guid.NewGuid(), PageSize: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSizeOver100_Fails()
    {
        var query = new GetIncomeExpenseListQuery(TenantId: Guid.NewGuid(), PageSize: 101);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
