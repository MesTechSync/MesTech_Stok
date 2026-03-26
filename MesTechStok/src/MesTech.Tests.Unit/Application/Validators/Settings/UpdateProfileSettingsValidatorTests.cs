using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateProfileSettingsValidatorTests
{
    private readonly UpdateProfileSettingsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new UpdateProfileSettingsCommand(Guid.NewGuid(), "Firma Adı", "1234567890");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new UpdateProfileSettingsCommand(Guid.Empty, "Firma", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyName_ShouldFail()
    {
        var cmd = new UpdateProfileSettingsCommand(Guid.NewGuid(), "", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameExceeds200_ShouldFail()
    {
        var cmd = new UpdateProfileSettingsCommand(Guid.NewGuid(), new string('N', 201), null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task TaxNumberExceeds20_ShouldFail()
    {
        var cmd = new UpdateProfileSettingsCommand(Guid.NewGuid(), "Firma", new string('T', 21));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public async Task NullTaxNumber_ShouldPass()
    {
        var cmd = new UpdateProfileSettingsCommand(Guid.NewGuid(), "Firma", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
