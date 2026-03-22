using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateAccountingBankAccountValidatorTests
{
    private readonly CreateAccountingBankAccountValidator _validator = new();

    private static CreateAccountingBankAccountCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        AccountName: "Isbank Ticari Hesap",
        Currency: "TRY",
        BankName: "Turkiye Is Bankasi",
        IBAN: "TR330006100519786457841326",
        AccountNumber: "519786457841326"
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyAccountName_FailsValidation()
    {
        var cmd = ValidCommand() with { AccountName = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountName");
    }

    [Fact]
    public async Task AccountNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { AccountName = new string('A', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountName");
    }

    [Fact]
    public async Task EmptyCurrency_FailsValidation()
    {
        var cmd = ValidCommand() with { Currency = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task CurrencyTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Currency = new string('C', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task BankNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { BankName = new string('B', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BankName");
    }

    [Fact]
    public async Task NullBankName_PassesValidation()
    {
        var cmd = ValidCommand() with { BankName = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task IBANTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { IBAN = new string('I', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IBAN");
    }

    [Fact]
    public async Task NullIBAN_PassesValidation()
    {
        var cmd = ValidCommand() with { IBAN = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AccountNumberTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { AccountNumber = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountNumber");
    }

    [Fact]
    public async Task NullAccountNumber_PassesValidation()
    {
        var cmd = ValidCommand() with { AccountNumber = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
