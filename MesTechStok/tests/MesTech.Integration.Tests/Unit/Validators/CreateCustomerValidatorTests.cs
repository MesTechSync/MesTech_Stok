using FluentAssertions;
using MesTech.Application.Commands.CreateCustomer;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateCustomerValidatorTests
{
    private readonly CreateCustomerValidator _validator = new();

    private static CreateCustomerCommand ValidCommand() => new(
        Name: "Ahmet Yılmaz",
        Code: "CUST-001",
        CustomerType: "INDIVIDUAL",
        ContactPerson: "Mehmet Demir",
        Email: "ahmet@example.com",
        Phone: "+90 555 123 4567",
        City: "İstanbul",
        TaxNumber: "1234567890",
        TaxOffice: "Kadıköy VD");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
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
    public void Name_Over_200_Chars_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('A', 201) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Empty_Code_Fails()
    {
        var cmd = ValidCommand() with { Code = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Code_Over_100_Chars_Fails()
    {
        var cmd = ValidCommand() with { Code = new string('C', 101) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var cmd = ValidCommand() with { Email = "invalid-email" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Null_Email_Passes()
    {
        var cmd = ValidCommand() with { Email = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Phone_Over_50_Chars_Fails()
    {
        var cmd = ValidCommand() with { Phone = new string('0', 51) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public void TaxNumber_Over_50_Chars_Fails()
    {
        var cmd = ValidCommand() with { TaxNumber = new string('9', 51) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public void TaxOffice_Over_200_Chars_Fails()
    {
        var cmd = ValidCommand() with { TaxOffice = new string('V', 201) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxOffice");
    }
}
