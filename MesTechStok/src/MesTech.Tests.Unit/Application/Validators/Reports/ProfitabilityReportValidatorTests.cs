using FluentAssertions;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

[Trait("Category", "Unit")]
public class ProfitabilityReportValidatorTests
{
    private readonly ProfitabilityReportValidator _sut = new();

    private static ProfitabilityReportQuery CreateValidQuery() =>
        new(
            TenantId: Guid.NewGuid(),
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 3, 31));

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
    public async Task ToDateBeforeFromDate_ShouldFail()
    {
        var query = CreateValidQuery() with
        {
            FromDate = new DateTime(2026, 6, 1),
            ToDate = new DateTime(2026, 1, 1)
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToDate");
    }

    [Fact]
    public async Task ToDateEqualsFromDate_ShouldFail()
    {
        var date = new DateTime(2026, 2, 15);
        var query = CreateValidQuery() with { FromDate = date, ToDate = date };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ToDateOneDayAfterFromDate_ShouldPass()
    {
        var query = CreateValidQuery() with
        {
            FromDate = new DateTime(2026, 3, 1),
            ToDate = new DateTime(2026, 3, 2)
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
            FromDate = new DateTime(2026, 12, 31),
            ToDate = new DateTime(2026, 1, 1)
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
