using FluentAssertions;
using MesTech.Application.Features.System.Queries.GetAuditLogs;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetAuditLogsValidatorTests
{
    private readonly GetAuditLogsValidator _validator = new();

    [Fact]
    public async Task ValidQuery_Passes()
    {
        var query = new GetAuditLogsQuery(TenantId: Guid.NewGuid());
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_Fails()
    {
        var query = new GetAuditLogsQuery(TenantId: Guid.Empty);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageZero_Fails()
    {
        var query = new GetAuditLogsQuery(TenantId: Guid.NewGuid(), Page: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSizeZero_Fails()
    {
        var query = new GetAuditLogsQuery(TenantId: Guid.NewGuid(), PageSize: 0);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSizeOver200_Fails()
    {
        var query = new GetAuditLogsQuery(TenantId: Guid.NewGuid(), PageSize: 201);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
