using FluentAssertions;
using MesTech.Application.Features.System.Queries.GetBackupHistory;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetBackupHistoryValidatorTests
{
    private readonly GetBackupHistoryValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetBackupHistoryQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetBackupHistoryQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LimitZero_Fails()
    {
        var query = new GetBackupHistoryQuery(TenantId: Guid.NewGuid(), Limit: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LimitOver100_Fails()
    {
        var query = new GetBackupHistoryQuery(TenantId: Guid.NewGuid(), Limit: 101);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
