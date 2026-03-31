using FluentAssertions;
using MesTech.Application.Features.Reports.CustomerSegmentReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

public class CustomerSegmentReportValidatorTests
{
    private readonly CustomerSegmentReportValidator _sut = new();

    private static CustomerSegmentReportQuery CreateValidQuery() => new(
        TenantId: Guid.NewGuid(),
        StartDate: new DateTime(2026, 1, 1),
        EndDate: new DateTime(2026, 3, 31));

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidQuery_ShouldPassValidation()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EmptyTenantId_ShouldFail()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
    public async Task EndDateEqualToStartDate_ShouldFail()
    {
        var sameDate = new DateTime(2026, 2, 15);
        var query = CreateValidQuery() with { StartDate = sameDate, EndDate = sameDate };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }

    [Fact]
    [Trait("Category", "Unit")]
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
    [Trait("Category", "Unit")]
    public async Task MultipleInvalidFields_ShouldReturnMultipleErrors()
    {
        var query = CreateValidQuery() with
        {
            TenantId = Guid.Empty,
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 1, 1)
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
