using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;
using MesTech.Application.Features.Hr.Commands.CreateTimeEntry;
using MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Documents.Commands.UploadDocument;
using MesTech.Application.Features.Auth.Commands.DisableMfa;
using MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region DeleteErpAccountMapping

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteErpAccountMappingValidatorTests
{
    private readonly DeleteErpAccountMappingValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeleteErpAccountMappingCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new DeleteErpAccountMappingCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_MappingId_Fails()
    {
        var cmd = new DeleteErpAccountMappingCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MappingId");
    }
}

#endregion

#region CreateTimeEntry

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateTimeEntryValidatorTests
{
    private readonly CreateTimeEntryValidator _validator = new();

    private static CreateTimeEntryCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        WorkTaskId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        StartedAt: DateTime.UtcNow.AddHours(-2),
        EndedAt: DateTime.UtcNow,
        HourlyRate: 150m,
        Description: "Stok modülü geliştirme");

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
    public void Empty_WorkTaskId_Fails()
    {
        var cmd = ValidCommand() with { WorkTaskId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WorkTaskId");
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var cmd = ValidCommand() with { UserId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Null_HourlyRate_Passes()
    {
        var cmd = ValidCommand() with { HourlyRate = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Zero_HourlyRate_Fails()
    {
        var cmd = ValidCommand() with { HourlyRate = 0m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HourlyRate");
    }

    [Fact]
    public void Null_Description_Passes()
    {
        var cmd = ValidCommand() with { Description = null };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Description_Over_500_Chars_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}

#endregion

#region UpdateJournalEntry

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateJournalEntryValidatorTests
{
    private readonly UpdateJournalEntryValidator _validator = new();

    private static UpdateJournalEntryCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        TenantId: Guid.NewGuid(),
        Description: "Satış faturası yevmiye",
        Lines: new List<JournalLineInput>
        {
            new(Guid.NewGuid(), 1000m, 0m),
            new(Guid.NewGuid(), 0m, 1000m)
        });

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
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
    public void Single_Line_Fails()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput> { new(Guid.NewGuid(), 1000m, 0m) }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void Unbalanced_Lines_Fails()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 1000m, 0m),
                new(Guid.NewGuid(), 0m, 500m)
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Balanced_Three_Lines_Passes()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 1000m, 0m),
                new(Guid.NewGuid(), 0m, 600m),
                new(Guid.NewGuid(), 0m, 400m)
            }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region DisableMfa (extended)

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DisableMfaValidatorTests
{
    private readonly DisableMfaValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DisableMfaCommand(Guid.NewGuid(), "123456");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var cmd = new DisableMfaCommand(Guid.Empty, "123456");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Empty_TotpCode_Fails()
    {
        var cmd = new DisableMfaCommand(Guid.NewGuid(), "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public void TotpCode_Too_Short_Fails()
    {
        var cmd = new DisableMfaCommand(Guid.NewGuid(), "12345");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public void TotpCode_Too_Long_Fails()
    {
        var cmd = new DisableMfaCommand(Guid.NewGuid(), "123456789");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotpCode");
    }

    [Fact]
    public void TotpCode_8_Chars_Passes()
    {
        var cmd = new DisableMfaCommand(Guid.NewGuid(), "12345678");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region ParseAndImportSettlement

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ParseAndImportSettlementValidatorTests
{
    private readonly ParseAndImportSettlementValidator _validator = new();

    private static ParseAndImportSettlementCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Platform: "Trendyol",
        RawData: new byte[] { 0x50, 0x4B, 0x03, 0x04 },
        Format: "xlsx");

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
    public void Empty_Platform_Fails()
    {
        var cmd = ValidCommand() with { Platform = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public void Empty_RawData_Fails()
    {
        var cmd = ValidCommand() with { RawData = Array.Empty<byte>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RawData");
    }

    [Fact]
    public void Empty_Format_Fails()
    {
        var cmd = ValidCommand() with { Format = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public void Unsupported_Format_Fails()
    {
        var cmd = ValidCommand() with { Format = "pdf" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }
}

#endregion
