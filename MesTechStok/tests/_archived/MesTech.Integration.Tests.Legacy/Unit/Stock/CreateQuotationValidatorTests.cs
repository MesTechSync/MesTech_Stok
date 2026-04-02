using FluentAssertions;
using MesTech.Application.Commands.CreateQuotation;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateQuotationValidatorTests
{
    private readonly CreateQuotationValidator _validator = new();

    private static CreateQuotationCommand ValidCommand() => new(
        QuotationNumber: "TEK-2026-001",
        ValidUntil: DateTime.UtcNow.AddDays(30),
        CustomerName: "Test Müşteri A.Ş.",
        CustomerTaxNumber: "1234567890",
        CustomerTaxOffice: "Kadıköy VD",
        CustomerAddress: "İstanbul",
        CustomerEmail: "info@test.com",
        Notes: "Teklif notu",
        Terms: "30 gün vadeli");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_QuotationNumber_Fails()
    {
        var cmd = ValidCommand() with { QuotationNumber = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationNumber");
    }

    [Fact]
    public void QuotationNumber_Over500_Fails()
    {
        var cmd = ValidCommand() with { QuotationNumber = new string('Q', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationNumber");
    }

    [Fact]
    public void Empty_CustomerName_Fails()
    {
        var cmd = ValidCommand() with { CustomerName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void CustomerName_Over500_Fails()
    {
        var cmd = ValidCommand() with { CustomerName = new string('C', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public void CustomerTaxNumber_Over500_Fails()
    {
        var cmd = ValidCommand() with { CustomerTaxNumber = new string('T', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerTaxNumber");
    }

    [Fact]
    public void CustomerTaxNumber_Null_Passes()
    {
        var cmd = ValidCommand() with { CustomerTaxNumber = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "CustomerTaxNumber");
    }

    [Fact]
    public void CustomerTaxOffice_Over500_Fails()
    {
        var cmd = ValidCommand() with { CustomerTaxOffice = new string('O', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerTaxOffice");
    }

    [Fact]
    public void CustomerAddress_Over500_Fails()
    {
        var cmd = ValidCommand() with { CustomerAddress = new string('A', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerAddress");
    }

    [Fact]
    public void CustomerEmail_Over500_Fails()
    {
        var cmd = ValidCommand() with { CustomerEmail = new string('E', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public void Notes_Over500_Fails()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Terms_Over500_Fails()
    {
        var cmd = ValidCommand() with { Terms = new string('T', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Terms");
    }

    [Fact]
    public void All_Optional_Null_Passes()
    {
        var cmd = ValidCommand() with
        {
            CustomerTaxNumber = null,
            CustomerTaxOffice = null,
            CustomerAddress = null,
            CustomerEmail = null,
            Notes = null,
            Terms = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
