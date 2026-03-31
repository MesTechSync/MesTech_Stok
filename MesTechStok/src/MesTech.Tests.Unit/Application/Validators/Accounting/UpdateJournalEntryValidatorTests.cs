using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
public class UpdateJournalEntryValidatorTests
{
    private readonly UpdateJournalEntryValidator _sut = new();

    private static UpdateJournalEntryCommand CreateValidCommand() => new(
        Id: Guid.NewGuid(),
        TenantId: Guid.NewGuid(),
        EntryDate: DateTime.UtcNow,
        Description: "Monthly depreciation entry",
        ReferenceNumber: "JE-2026-001",
        Lines: new List<JournalLineInput>
        {
            new(AccountId: Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: "Debit line"),
            new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: "Credit line")
        },
        RowVersion: null);

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_ShouldFail()
    {
        var command = CreateValidCommand() with { Id = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyDescription_ShouldFail()
    {
        var command = CreateValidCommand() with { Description = "" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_ExceedsMaxLength_ShouldFail()
    {
        var command = CreateValidCommand() with { Description = new string('x', 501) };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task EmptyLines_ShouldFail()
    {
        var command = CreateValidCommand() with { Lines = new List<JournalLineInput>() };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public async Task SingleLine_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(AccountId: Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: null)
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public async Task UnbalancedLines_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(AccountId: Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: 500m, Description: null)
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Line_WithEmptyAccountId_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(AccountId: Guid.Empty, Debit: 1000m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: null)
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Line_NegativeDebit_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(AccountId: Guid.NewGuid(), Debit: -100m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: null)
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Line_NegativeCredit_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(AccountId: Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: -1000m, Description: null)
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Line_BothDebitAndCreditZero_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: null)
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task NullReferenceNumber_ShouldPass()
    {
        var command = CreateValidCommand() with { ReferenceNumber = null };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleBalancedLines_ShouldPass()
    {
        var command = CreateValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(AccountId: Guid.NewGuid(), Debit: 500m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 500m, Credit: 0m, Description: null),
                new(AccountId: Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: null)
            }
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Description_AtMaxLength_ShouldPass()
    {
        var command = CreateValidCommand() with { Description = new string('d', 500) };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
