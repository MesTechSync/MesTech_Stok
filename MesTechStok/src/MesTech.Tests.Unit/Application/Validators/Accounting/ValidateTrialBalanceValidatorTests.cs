using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.ValidateTrialBalance;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
public class ValidateTrialBalanceValidatorTests
{
    private readonly ValidateTrialBalanceValidator _sut = new();

    private static ValidateTrialBalanceQuery CreateValidQuery() =>
        new(
            TenantId: Guid.NewGuid(),
            StartDate: new DateTime(2026, 1, 1),
            EndDate: new DateTime(2026, 3, 31));

    [Fact]
    public async Task ValidQuery_ShouldPassValidation()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task StartDateAfterEndDate_ShouldFail()
    {
        var query = CreateValidQuery() with
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 1, 1)
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartDate");
    }

    [Fact]
    public async Task StartDateEqualsEndDate_ShouldPass()
    {
        var date = new DateTime(2026, 3, 15);
        var query = CreateValidQuery() with { StartDate = date, EndDate = date };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task StartDateOneDayBeforeEndDate_ShouldPass()
    {
        var query = CreateValidQuery() with
        {
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 2)
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothTenantAndDateInvalid_ShouldHaveMultipleErrors()
    {
        var query = CreateValidQuery() with
        {
            TenantId = Guid.Empty,
            StartDate = new DateTime(2026, 12, 31),
            EndDate = new DateTime(2026, 1, 1)
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task ValidQuery_ShouldHaveNoErrors()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.Errors.Should().BeEmpty();
    }
}
