using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetFeedImportHistoryQueryValidatorTests
{
    private readonly GetFeedImportHistoryQueryValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFeedId_ShouldFail()
    {
        var input = CreateValidQuery() with { FeedId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FeedId");
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

    private static GetFeedImportHistoryQuery CreateValidQuery() => new(FeedId: Guid.NewGuid(), Page: 1, PageSize: 20);
}
