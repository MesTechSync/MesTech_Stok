using FluentAssertions;
using MesTech.Application.Features.Stores.Queries.GetStoreCredential;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Stores;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetStoreCredentialValidatorTests
{
    private readonly GetStoreCredentialValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyStoreId_ShouldFail()
    {
        var input = CreateValidQuery() with { StoreId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }

    private static GetStoreCredentialQuery CreateValidQuery() => new(StoreId: Guid.NewGuid());
}
