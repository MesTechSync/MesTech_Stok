using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.ValidateBalanceSheet;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
public class ValidateBalanceSheetValidatorTests
{
    private readonly ValidateBalanceSheetValidator _sut = new();

    private static ValidateBalanceSheetQuery CreateValidQuery() =>
        new(TenantId: Guid.NewGuid(), AsOfDate: DateTime.UtcNow);

    [Fact]
    public async Task ValidQuery_ShouldPassValidation()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task DefaultTenantId_ShouldFail()
    {
        var query = new ValidateBalanceSheetQuery(TenantId: default, AsOfDate: DateTime.UtcNow);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidTenantId_ShouldHaveNoErrors()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldHaveExactlyOneError()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.Errors.Should().HaveCount(1);
    }
}
