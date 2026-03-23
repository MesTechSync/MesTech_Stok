using FluentAssertions;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetProfileSettingsValidatorTests
{
    private readonly GetProfileSettingsValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = new GetProfileSettingsQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var query = new GetProfileSettingsQuery(Guid.Empty);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
