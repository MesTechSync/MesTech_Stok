using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class ImportBankStatementValidatorTests
{
    private readonly ImportBankStatementValidator _validator = new();

    private static ImportBankStatementCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        BankAccountId: Guid.NewGuid(),
        Transactions: new List<BankTransactionInput>
        {
            new(
                TransactionDate: new DateTime(2026, 3, 15),
                Amount: 5000m,
                Description: "Trendyol hakedis odemesi"
            )
        }
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
    public async Task EmptyBankAccountId_FailsValidation()
    {
        var cmd = ValidCommand() with { BankAccountId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BankAccountId");
    }
}
