using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateJournalEntryValidatorTests
{
    private readonly CreateJournalEntryValidator _validator = new();

    private static CreateJournalEntryCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        EntryDate: DateTime.UtcNow,
        Description: "Test journal entry",
        ReferenceNumber: "JE-001",
        Lines: new List<JournalLineInput>
        {
            new(Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: "Debit line"),
            new(Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: "Credit line")
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
    public void Empty_Description_Fails()
    {
        var cmd = ValidCommand() with { Description = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Description_Over500_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('x', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Empty_Lines_Fails()
    {
        var cmd = ValidCommand() with { Lines = new List<JournalLineInput>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Single_Line_Fails()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: 100m, Credit: 0m, Description: "Only one line")
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Unbalanced_DebitCredit_Fails()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: "Debit"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 500m, Description: "Credit — less")
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Balanced_ThreeLines_Passes()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: 1000m, Credit: 0m, Description: "D1"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 600m, Description: "C1"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 400m, Description: "C2")
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Line_With_Empty_AccountId_Fails()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.Empty, Debit: 1000m, Credit: 0m, Description: "Bad account"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 1000m, Description: "Good")
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Line_With_Both_Zero_DebitCredit_Fails()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: 0m, Credit: 0m, Description: "Zero line"),
                new(Guid.NewGuid(), Debit: 0m, Credit: 0m, Description: "Zero line 2")
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Line_With_Negative_Debit_Fails()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), Debit: -100m, Credit: 0m, Description: "Negative"),
                new(Guid.NewGuid(), Debit: 0m, Credit: -100m, Description: "Negative credit")
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}
