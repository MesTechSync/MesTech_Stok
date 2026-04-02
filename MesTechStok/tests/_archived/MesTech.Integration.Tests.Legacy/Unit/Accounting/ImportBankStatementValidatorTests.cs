using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.ImportBankStatement;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ImportBankStatementValidatorTests
{
    private readonly ImportBankStatementValidator _validator = new();

    private static ImportBankStatementCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        BankAccountId: Guid.NewGuid(),
        Transactions: new List<BankTransactionInput>
        {
            new(DateTime.UtcNow, 1000m, "Havale geldi", "REF-001", "KEY-001")
        });

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
    public void Empty_BankAccountId_Fails()
    {
        var cmd = ValidCommand() with { BankAccountId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BankAccountId");
    }

    [Fact]
    public void Empty_Transactions_Passes()
    {
        var cmd = ValidCommand() with { Transactions = new List<BankTransactionInput>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
