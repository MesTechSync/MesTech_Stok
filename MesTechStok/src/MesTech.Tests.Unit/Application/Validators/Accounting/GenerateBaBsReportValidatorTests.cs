using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

public class GenerateBaBsReportValidatorTests
{
    private readonly GenerateBaBsReportValidator _sut = new();

    private static GenerateBaBsReportQuery CreateValidQuery() => new(
        TenantId: Guid.NewGuid(),
        Year: 2026,
        Month: 3);

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

    [Theory]
    [InlineData(1999)]
    [InlineData(2100)]
    [Trait("Category", "Unit")]
    public async Task YearOutOfRange_ShouldFail(int year)
    {
        var query = CreateValidQuery() with { Year = year };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Year");
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2050)]
    [InlineData(2099)]
    [Trait("Category", "Unit")]
    public async Task YearWithinRange_ShouldPass(int year)
    {
        var query = CreateValidQuery() with { Year = year };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    [Trait("Category", "Unit")]
    public async Task MonthOutOfRange_ShouldFail(int month)
    {
        var query = CreateValidQuery() with { Month = month };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Month");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    [Trait("Category", "Unit")]
    public async Task MonthWithinRange_ShouldPass(int month)
    {
        var query = CreateValidQuery() with { Month = month };
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
            Year = 1900,
            Month = 15
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
    }
}
