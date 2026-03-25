using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateAccountingBankAccountValidatorTests
{
    private readonly CreateAccountingBankAccountValidator _validator = new();

    private static CreateAccountingBankAccountCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        AccountName: "İş Bankası Ticari",
        Currency: "TRY",
        BankName: "Türkiye İş Bankası",
        IBAN: "TR330006100519786457841326",
        AccountNumber: "5197864578");

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
    public void Empty_AccountName_Fails()
    {
        var cmd = ValidCommand() with { AccountName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountName");
    }

    [Fact]
    public void AccountName_Over500_Fails()
    {
        var cmd = ValidCommand() with { AccountName = new string('A', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountName");
    }

    [Fact]
    public void Empty_Currency_Fails()
    {
        var cmd = ValidCommand() with { Currency = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void Currency_Over500_Fails()
    {
        var cmd = ValidCommand() with { Currency = new string('C', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void BankName_Over500_Fails()
    {
        var cmd = ValidCommand() with { BankName = new string('B', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BankName");
    }

    [Fact]
    public void BankName_Null_Passes()
    {
        var cmd = ValidCommand() with { BankName = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "BankName");
    }

    [Fact]
    public void IBAN_Over500_Fails()
    {
        var cmd = ValidCommand() with { IBAN = new string('I', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IBAN");
    }

    [Fact]
    public void IBAN_Null_Passes()
    {
        var cmd = ValidCommand() with { IBAN = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "IBAN");
    }

    [Fact]
    public void AccountNumber_Over500_Fails()
    {
        var cmd = ValidCommand() with { AccountNumber = new string('N', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountNumber");
    }

    [Fact]
    public void AccountNumber_Null_Passes()
    {
        var cmd = ValidCommand() with { AccountNumber = null };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "AccountNumber");
    }
}
