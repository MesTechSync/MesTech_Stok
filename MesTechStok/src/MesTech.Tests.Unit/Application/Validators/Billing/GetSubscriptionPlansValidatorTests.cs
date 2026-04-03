using FluentAssertions;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Billing;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetSubscriptionPlansValidatorTests
{
    private readonly GetSubscriptionPlansValidator _sut = new();

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
        var input = new GetSubscriptionPlansQuery();
        var result = await _sut.ValidateAsync(input);
        result.Errors.Should().BeEmpty();
    }

    private static GetSubscriptionPlansQuery CreateValidQuery() => new();
}
