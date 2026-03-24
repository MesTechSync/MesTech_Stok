using FluentAssertions;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;

namespace MesTech.Tests.Unit.Application.Validators.Erp;

[Trait("Category", "Unit")]
[Trait("Feature", "Erp")]
public class GetErpSyncLogsValidatorTests
{
    private readonly GetErpSyncLogsValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetErpSyncLogsQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetErpSyncLogsQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageZero_Fails()
    {
        var query = new GetErpSyncLogsQuery(TenantId: Guid.NewGuid(), Page: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSizeOver100_Fails()
    {
        var query = new GetErpSyncLogsQuery(TenantId: Guid.NewGuid(), PageSize: 101);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
