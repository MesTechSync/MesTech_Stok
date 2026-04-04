using FluentAssertions;
using MesTech.Application.Queries.GetExpenses;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetExpensesValidatorTests
{
    private readonly GetExpensesValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultQuery_ShouldProduceNoErrors()
    {
        var input = new GetExpensesQuery();
        var result = await _sut.ValidateAsync(input);
        result.Errors.Should().BeEmpty();
    }

    private static GetExpensesQuery CreateValidQuery() => new();
}
