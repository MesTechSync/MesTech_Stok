using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateStoreSettingsValidatorTests
{
    private readonly UpdateStoreSettingsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), "MesTech Ltd.", "1234567890", "05551234567", "info@mestech.com", "İstanbul");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.Empty, "Firma", null, null, null, null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyCompanyName_ShouldFail()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), "", null, null, null, null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task CompanyNameExceeds500_ShouldFail()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), new string('C', 501), null, null, null, null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task InvalidEmail_ShouldFail()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), "Firma", null, null, "bad", null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task PhoneExceeds50_ShouldFail()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), "Firma", null, new string('1', 51), null, null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public async Task TaxNumberExceeds50_ShouldFail()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), "Firma", new string('T', 51), null, null, null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public async Task AllNullOptionals_ShouldPass()
    {
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), "Firma", null, null, null, null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
