using FluentAssertions;
using MesTech.Application.Features.Hr.Queries.GetTimeEntries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Hr;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetTimeEntriesValidatorTests
{
    private readonly GetTimeEntriesValidator _sut = new();

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

    private static GetTimeEntriesQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), From: DateTime.UtcNow.AddMonths(-1), To: DateTime.UtcNow, Page: 1, PageSize: 20);
}
