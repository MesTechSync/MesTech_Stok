using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Dropshipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetFeedSourceByIdQueryValidatorTests
{
    private readonly GetFeedSourceByIdQueryValidator _sut = new();

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

    private static GetFeedSourceByIdQuery CreateValidQuery() => new(FeedId: Guid.NewGuid());
}
