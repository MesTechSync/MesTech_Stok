using FluentAssertions;
using MesTech.Application.Queries.ListOrders;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Orders;

[Trait("Category", "Unit")]
public class ListOrdersValidatorTests
{
    private readonly ListOrdersValidator _sut = new();

    private static ListOrdersQuery CreateValidQuery() =>
        new(Status: "Pending");

    [Fact]
    public async Task ValidQuery_ShouldPassValidation()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NullStatus_ShouldPass()
    {
        var query = CreateValidQuery() with { Status = null };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task StatusExactly50Chars_ShouldPass()
    {
        var query = CreateValidQuery() with { Status = new string('A', 50) };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task StatusExceeds50Chars_ShouldFail()
    {
        var query = CreateValidQuery() with { Status = new string('A', 51) };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public async Task EmptyStatus_ShouldPass()
    {
        var query = CreateValidQuery() with { Status = "" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultPageValues_ShouldPass()
    {
        var query = new ListOrdersQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
