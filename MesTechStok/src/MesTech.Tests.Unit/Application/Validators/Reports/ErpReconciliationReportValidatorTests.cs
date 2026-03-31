using FluentAssertions;
using MesTech.Application.Features.Reports.ErpReconciliationReport;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

public class ErpReconciliationReportValidatorTests
{
    private readonly ErpReconciliationReportValidator _sut = new();

    private static ErpReconciliationReportQuery CreateValidQuery() => new(
        TenantId: Guid.NewGuid(),
        ErpProvider: ErpProvider.Parasut);

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
    public async Task InvalidErpProvider_ShouldFail()
    {
        var query = CreateValidQuery() with { ErpProvider = (ErpProvider)999 };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpProvider");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidErpProvider_ShouldPass()
    {
        var query = CreateValidQuery() with { ErpProvider = ErpProvider.Parasut };
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
            ErpProvider = (ErpProvider)999
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
