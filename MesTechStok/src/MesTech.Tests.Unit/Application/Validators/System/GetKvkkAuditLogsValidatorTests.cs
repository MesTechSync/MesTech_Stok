using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetKvkkAuditLogsValidatorTests
{
    private readonly GetKvkkAuditLogsValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var input = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Page_WhenZero_ShouldFail()
    {
        var input = CreateValidQuery() with { Page = 0 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSize_WhenZero_ShouldFail()
    {
        var input = CreateValidQuery() with { PageSize = 0 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
    }

    private static GetKvkkAuditLogsQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), Page: 1, PageSize: 20);
}
