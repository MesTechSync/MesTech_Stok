using FluentAssertions;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetGeneralSettingsValidatorTests
{
    private readonly GetGeneralSettingsValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = new GetGeneralSettingsQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var query = new GetGeneralSettingsQuery(Guid.Empty);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
