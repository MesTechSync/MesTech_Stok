using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

public class ExportPersonalDataValidatorTests
{
    private readonly ExportPersonalDataValidator _sut = new();

    private static ExportPersonalDataQuery CreateValidQuery() => new(
        TenantId: Guid.NewGuid(),
        RequestedByUserId: Guid.NewGuid());

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
    public async Task EmptyRequestedByUserId_ShouldFail()
    {
        var query = CreateValidQuery() with { RequestedByUserId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequestedByUserId");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DefaultGuidRequestedByUserId_ShouldFail()
    {
        var query = CreateValidQuery() with { RequestedByUserId = default };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task BothFieldsEmpty_ShouldReturnMultipleErrors()
    {
        var query = CreateValidQuery() with
        {
            TenantId = Guid.Empty,
            RequestedByUserId = Guid.Empty
        };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidGuids_ShouldHaveNoErrors()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.Errors.Should().BeEmpty();
    }
}
