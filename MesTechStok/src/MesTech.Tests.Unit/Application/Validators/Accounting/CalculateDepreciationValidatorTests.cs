using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

public class CalculateDepreciationValidatorTests
{
    private readonly CalculateDepreciationValidator _sut = new();

    private static CalculateDepreciationQuery CreateValidQuery() => new(
        AssetId: Guid.NewGuid());

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
    public async Task EmptyAssetId_ShouldFail()
    {
        var query = CreateValidQuery() with { AssetId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AssetId");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DefaultGuidAssetId_ShouldFail()
    {
        var query = new CalculateDepreciationQuery(default);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task NewGuidAssetId_ShouldPass()
    {
        var query = new CalculateDepreciationQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SpecificValidGuid_ShouldPass()
    {
        var query = CreateValidQuery() with { AssetId = Guid.Parse("11111111-1111-1111-1111-111111111111") };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
