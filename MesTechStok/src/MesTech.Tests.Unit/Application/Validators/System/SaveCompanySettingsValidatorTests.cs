using FluentAssertions;
using MesTech.Application.Commands.SaveCompanySettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SaveCompanySettingsValidatorTests
{
    private readonly SaveCompanySettingsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CompanyName_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CompanyName = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task CompanyName_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CompanyName = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyName");
    }

    [Fact]
    public async Task TaxNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TaxNumber = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public async Task TaxNumber_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { TaxNumber = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Phone_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Phone = new string('P', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public async Task Phone_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Phone = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Email_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Email = new string('E', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Email_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Email = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Address_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Address = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Address");
    }

    [Fact]
    public async Task Address_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Address = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static SaveCompanySettingsCommand CreateValidCommand() => new(
        CompanyName: "MesTech A.S.",
        TaxNumber: "1234567890",
        Phone: "+902121234567",
        Email: "info@mestech.com",
        Address: "Istanbul, Turkey",
        Warehouses: new List<WarehouseInput>()
    );
}
