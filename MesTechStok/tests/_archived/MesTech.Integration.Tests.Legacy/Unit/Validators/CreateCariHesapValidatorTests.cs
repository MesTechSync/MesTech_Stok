using FluentAssertions;
using MesTech.Application.Commands.CreateCariHesap;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCariHesapValidatorTests
{
    private readonly CreateCariHesapValidator _validator = new();

    private static CreateCariHesapCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "ABC Ticaret Ltd.",
        TaxNumber: "1234567890",
        Type: CariHesapType.Customer,
        Phone: "+90 212 555 0000",
        Email: "info@abc.com",
        Address: "İstanbul, Kadıköy");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('A', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Invalid_Type_Enum_Fails()
    {
        var cmd = ValidCommand() with { Type = (CariHesapType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void TaxNumber_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { TaxNumber = new string('9', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with
        {
            TaxNumber = null,
            Phone = null,
            Email = null,
            Address = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
