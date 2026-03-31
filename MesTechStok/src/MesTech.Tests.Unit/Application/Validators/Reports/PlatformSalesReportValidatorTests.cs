using FluentAssertions;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

[Trait("Category", "Unit")]
public class PlatformSalesReportValidatorTests
{
    private readonly PlatformSalesReportValidator _sut = new();

    private static PlatformSalesReportQuery CreateValidQuery() =>
        new(
            TenantId: Guid.NewGuid(),
            StartDate: new DateTime(2026, 1, 1),
            EndDate: new DateTime(2026, 3, 31),
            PlatformFilter: null);

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
    public async Task NullPlatformFilter_ShouldPass()
    {
        var query = CreateValidQuery() with { PlatformFilter = null };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PlatformFilterExactly50Chars_ShouldPass()
    {
        var query = CreateValidQuery() with { PlatformFilter = new string('T', 50) };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PlatformFilterExceeds50Chars_ShouldFail()
    {
        var query = CreateValidQuery() with { PlatformFilter = new string('T', 51) };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlatformFilter");
    }

    [Fact]
    public async Task ValidPlatformFilter_ShouldPass()
    {
        var query = CreateValidQuery() with { PlatformFilter = "Trendyol" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AllFieldsInvalid_ShouldHaveMultipleErrors()
    {
        var query = new PlatformSalesReportQuery(
            TenantId: Guid.Empty,
            StartDate: new DateTime(2026, 12, 31),
            EndDate: new DateTime(2026, 1, 1),
            PlatformFilter: new string('X', 51));
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
    }
}
