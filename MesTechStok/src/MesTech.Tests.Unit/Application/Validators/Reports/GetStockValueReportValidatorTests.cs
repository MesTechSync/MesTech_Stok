using FluentAssertions;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Reports;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetStockValueReportValidatorTests
{
    private readonly GetStockValueReportValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = new GetStockValueReportQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var query = new GetStockValueReportQuery(Guid.Empty);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task NullCategoryFilter_ShouldPass()
    {
        var query = new GetStockValueReportQuery(Guid.NewGuid(), null);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
