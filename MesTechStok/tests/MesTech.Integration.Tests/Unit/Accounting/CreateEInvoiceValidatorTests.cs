using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateEInvoiceValidatorTests
{
    private readonly CreateEInvoiceValidator _validator = new();

    private static CreateEInvoiceCommand ValidCommand() => new(
        OrderId: Guid.NewGuid(),
        BuyerVkn: "1234567890",
        BuyerTitle: "Test Firma A.Ş.",
        BuyerEmail: "muhasebe@test.com",
        Scenario: EInvoiceScenario.TEMELFATURA,
        Type: EInvoiceType.SATIS,
        IssueDate: DateTime.UtcNow,
        CurrencyCode: "TRY",
        Lines: new List<CreateEInvoiceLineRequest>
        {
            new("Ürün A", 1, "C62", 100m, 18, 0, Guid.NewGuid())
        },
        ProviderId: "sovos");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuyerVkn_Fails()
    {
        var cmd = ValidCommand() with { BuyerVkn = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerVkn");
    }

    [Fact]
    public void BuyerVkn_Over500_Fails()
    {
        var cmd = ValidCommand() with { BuyerVkn = new string('1', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerVkn");
    }

    [Fact]
    public void Empty_BuyerTitle_Fails()
    {
        var cmd = ValidCommand() with { BuyerTitle = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerTitle");
    }

    [Fact]
    public void BuyerTitle_Over500_Fails()
    {
        var cmd = ValidCommand() with { BuyerTitle = new string('T', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerTitle");
    }

    [Fact]
    public void BuyerEmail_Over500_Fails()
    {
        var cmd = ValidCommand() with { BuyerEmail = new string('e', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerEmail");
    }

    [Fact]
    public void BuyerEmail_Null_Passes()
    {
        var cmd = ValidCommand() with { BuyerEmail = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "BuyerEmail");
    }

    [Fact]
    public void Invalid_Type_Fails()
    {
        var cmd = ValidCommand() with { Type = (EInvoiceType)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void Empty_CurrencyCode_Fails()
    {
        var cmd = ValidCommand() with { CurrencyCode = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public void Empty_ProviderId_Fails()
    {
        var cmd = ValidCommand() with { ProviderId = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProviderId");
    }
}
