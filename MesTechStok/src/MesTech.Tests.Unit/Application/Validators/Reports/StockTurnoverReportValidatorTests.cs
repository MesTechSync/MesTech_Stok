using FluentAssertions;
using MesTech.Application.Features.Reports.StockTurnoverReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

[Trait("Category", "Unit")]
public class StockTurnoverReportValidatorTests
{
    private readonly StockTurnoverReportValidator _sut = new();

    private static StockTurnoverReportQuery CreateValidQuery() =>
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
    public async Task EndDateBeforeStartDate_ShouldFail()
    {
        var query = CreateValidQuery() with
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 1, 1)
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }

    [Fact]
    public async Task EndDateEqualsStartDate_ShouldFail()
    {
        var date = new DateTime(2026, 2, 15);
        var query = CreateValidQuery() with { StartDate = date, EndDate = date };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EndDateOneDayAfterStartDate_ShouldPass()
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
}
