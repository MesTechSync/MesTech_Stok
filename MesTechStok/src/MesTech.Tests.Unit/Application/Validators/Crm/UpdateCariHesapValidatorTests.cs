using FluentAssertions;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateCariHesapValidatorTests
{
    private readonly UpdateCariHesapValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Id_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Id = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task TaxNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TaxNumber = new string('1', 501) };
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
    public async Task Type_WhenInvalid_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Type = (CariHesapType)99 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task Phone_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Phone = new string('0', 501) };
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
        var cmd = CreateValidCommand() with { Email = new string('e', 501) };
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

    private static UpdateCariHesapCommand CreateValidCommand() => new(
        Id: Guid.NewGuid(),
        Name: "Guncellenmis Firma",
        TaxNumber: "9876543210",
        Type: CariHesapType.Tedarikci,
        Phone: "+905559876543",
        Email: "update@test.com",
        Address: "Ankara, Turkiye"
    );
}
