using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateJournalEntryValidatorTests
{
    private readonly CreateJournalEntryValidator _validator = new();

    private static CreateJournalEntryCommand ValidCommand()
    {
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();
        return new CreateJournalEntryCommand(
            TenantId: Guid.NewGuid(),
            EntryDate: new DateTime(2026, 3, 15),
            Description: "Kira odemesi",
            ReferenceNumber: "JE-2026-001",
            Lines: new List<JournalLineInput>
            {
                new(accountId1, 15000m, 0m, "Kira gideri"),
                new(accountId2, 0m, 15000m, "Banka hesabi")
            }
        );
    }

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
    }

    [Fact]
    public async Task EmptyDescription_FailsValidation()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DescriptionTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyLines_FailsValidation()
    {
        var cmd = ValidCommand() with { Lines = new List<JournalLineInput>() };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SingleLine_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 1000m, 0m, "Single line")
            }
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UnbalancedDebitCredit_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 1000m, 0m, "Debit"),
                new(Guid.NewGuid(), 0m, 500m, "Credit - unbalanced")
            }
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LineWithEmptyAccountId_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.Empty, 1000m, 0m, "Invalid account"),
                new(Guid.NewGuid(), 0m, 1000m, "Valid account")
            }
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LineWithNegativeDebit_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), -100m, 0m, "Negative debit"),
                new(Guid.NewGuid(), 0m, -100m, "Negative credit")
            }
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LineWithBothZeroDebitAndCredit_FailsValidation()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 0m, 0m, "Zero both"),
                new(Guid.NewGuid(), 0m, 0m, "Zero both again")
            }
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ThreeLineBalanced_PassesValidation()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 1000m, 0m, "Debit 1"),
                new(Guid.NewGuid(), 500m, 0m, "Debit 2"),
                new(Guid.NewGuid(), 0m, 1500m, "Credit")
            }
        };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
