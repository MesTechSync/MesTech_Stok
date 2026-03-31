using FluentAssertions;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

public class InventoryValuationReportValidatorTests
{
    private readonly InventoryValuationReportValidator _sut = new();

    private static InventoryValuationReportQuery CreateValidQuery() => new(
        TenantId: Guid.NewGuid());

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
    public async Task DefaultGuidTenantId_ShouldFail()
    {
        var query = new InventoryValuationReportQuery(default);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task NewGuidTenantId_ShouldPass()
    {
        var query = new InventoryValuationReportQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SpecificValidGuid_ShouldPass()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
