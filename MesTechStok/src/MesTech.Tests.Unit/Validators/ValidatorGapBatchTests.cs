using System.IO;
using System.Text;
using FluentAssertions;
using FluentValidation.TestHelper;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;
using MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;
using MesTech.Application.Features.Auth.Commands.DisableMfa;
using MesTech.Application.Features.Documents.Commands.UploadDocument;
using MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;
using MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;
using MesTech.Application.Features.Hr.Commands.CreateTimeEntry;
using MesTech.Application.Features.Stock.Commands.CreateStockLot;

namespace MesTech.Tests.Unit.Validators;

// ════════════════════════════════════════════════════════
// DEV5 TUR 4: Validator gap batch — 8 validator tests
// Closes validator coverage gap from ~77% → 100%
// ════════════════════════════════════════════════════════

#region CreateErpAccountMappingValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class CreateErpAccountMappingValidatorTests
{
    private readonly CreateErpAccountMappingValidator _sut = new();

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(new CreateErpAccountMappingCommand(Guid.NewGuid(), "120", "Alicilar", "Gelir", "A-120", "Alicilar ERP"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyTenantId_Fails()
        => _sut.TestValidate(new CreateErpAccountMappingCommand(Guid.Empty, "120", "X", "T", "A", "B"))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void EmptyMesTechCode_Fails()
        => _sut.TestValidate(new CreateErpAccountMappingCommand(Guid.NewGuid(), "", "X", "T", "A", "B"))
            .ShouldHaveValidationErrorFor(x => x.MesTechCode);

    [Fact]
    public void EmptyErpCode_Fails()
        => _sut.TestValidate(new CreateErpAccountMappingCommand(Guid.NewGuid(), "120", "X", "T", "", "B"))
            .ShouldHaveValidationErrorFor(x => x.ErpCode);
}

#endregion

#region DeleteErpAccountMappingValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class DeleteErpAccountMappingValidatorTests
{
    private readonly DeleteErpAccountMappingValidator _sut = new();

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(new DeleteErpAccountMappingCommand(Guid.NewGuid(), Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyTenantId_Fails()
        => _sut.TestValidate(new DeleteErpAccountMappingCommand(Guid.Empty, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void EmptyMappingId_Fails()
        => _sut.TestValidate(new DeleteErpAccountMappingCommand(Guid.NewGuid(), Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.MappingId);
}

#endregion

#region CreateStockLotValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class CreateStockLotValidatorTests
{
    private readonly CreateStockLotValidator _sut = new();

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(new CreateStockLotCommand(Guid.NewGuid(), Guid.NewGuid(), "LOT-001", 100, 25.50m))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ZeroQuantity_Fails()
        => _sut.TestValidate(new CreateStockLotCommand(Guid.NewGuid(), Guid.NewGuid(), "LOT-001", 0, 10m))
            .ShouldHaveValidationErrorFor(x => x.Quantity);

    [Fact]
    public void NegativeUnitCost_Fails()
        => _sut.TestValidate(new CreateStockLotCommand(Guid.NewGuid(), Guid.NewGuid(), "LOT-001", 10, -5m))
            .ShouldHaveValidationErrorFor(x => x.UnitCost);

    [Fact]
    public void EmptyLotNumber_Fails()
        => _sut.TestValidate(new CreateStockLotCommand(Guid.NewGuid(), Guid.NewGuid(), "", 10, 10m))
            .ShouldHaveValidationErrorFor(x => x.LotNumber);
}

#endregion

#region CreateTimeEntryValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class CreateTimeEntryValidatorTests
{
    private readonly CreateTimeEntryValidator _sut = new();

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(new CreateTimeEntryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyTenantId_Fails()
        => _sut.TestValidate(new CreateTimeEntryCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.TenantId);

    [Fact]
    public void EmptyWorkTaskId_Fails()
        => _sut.TestValidate(new CreateTimeEntryCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()))
            .ShouldHaveValidationErrorFor(x => x.WorkTaskId);

    [Fact]
    public void ZeroHourlyRate_Fails()
        => _sut.TestValidate(new CreateTimeEntryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), HourlyRate: 0m))
            .ShouldHaveValidationErrorFor(x => x.HourlyRate);
}

#endregion

#region DisableMfaValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class DisableMfaValidatorTests
{
    private readonly DisableMfaValidator _sut = new();

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(new DisableMfaCommand(Guid.NewGuid(), "123456"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyUserId_Fails()
        => _sut.TestValidate(new DisableMfaCommand(Guid.Empty, "123456"))
            .ShouldHaveValidationErrorFor(x => x.UserId);

    [Fact]
    public void EmptyTotpCode_Fails()
        => _sut.TestValidate(new DisableMfaCommand(Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(x => x.TotpCode);

    [Fact]
    public void TooShortTotpCode_Fails()
        => _sut.TestValidate(new DisableMfaCommand(Guid.NewGuid(), "123"))
            .ShouldHaveValidationErrorFor(x => x.TotpCode);
}

#endregion

#region ParseAndImportSettlementValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class ParseAndImportSettlementValidatorTests
{
    private readonly ParseAndImportSettlementValidator _sut = new();

    private static byte[] ToBytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(new ParseAndImportSettlementCommand(Guid.NewGuid(), "Trendyol", ToBytes("data"), "JSON"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyRawData_Fails()
        => _sut.TestValidate(new ParseAndImportSettlementCommand(Guid.NewGuid(), "Trendyol", Array.Empty<byte>(), "JSON"))
            .ShouldHaveValidationErrorFor(x => x.RawData);

    [Fact]
    public void EmptyPlatform_Fails()
        => _sut.TestValidate(new ParseAndImportSettlementCommand(Guid.NewGuid(), "", ToBytes("data"), "JSON"))
            .ShouldHaveValidationErrorFor(x => x.Platform);

    [Theory]
    [InlineData("JSON")]
    [InlineData("TSV")]
    [InlineData("CSV")]
    [InlineData("XML")]
    public void ValidFormat_Passes(string format)
        => _sut.TestValidate(new ParseAndImportSettlementCommand(Guid.NewGuid(), "HB", ToBytes("d"), format))
            .ShouldNotHaveValidationErrorFor(x => x.Format);

    [Fact]
    public void InvalidFormat_Fails()
        => _sut.TestValidate(new ParseAndImportSettlementCommand(Guid.NewGuid(), "HB", ToBytes("d"), "YAML"))
            .ShouldHaveValidationErrorFor(x => x.Format);
}

#endregion

#region UploadDocumentValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class UploadDocumentValidatorTests
{
    private readonly UploadDocumentValidator _sut = new();

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(new UploadDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "invoice.pdf", "application/pdf", 1024, new MemoryStream(new byte[] { 1, 2, 3 })))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyFileName_Fails()
        => _sut.TestValidate(new UploadDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "", "application/pdf", 1024, new MemoryStream(new byte[] { 1 })))
            .ShouldHaveValidationErrorFor(x => x.FileName);

    [Fact]
    public void InvalidContentType_Fails()
        => _sut.TestValidate(new UploadDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "virus.exe", "application/octet-stream", 1024, new MemoryStream(new byte[] { 1 })))
            .ShouldHaveValidationErrorFor(x => x.ContentType);

    [Fact]
    public void ZeroFileSize_Fails()
        => _sut.TestValidate(new UploadDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "doc.pdf", "application/pdf", 0, new MemoryStream(new byte[] { 1 })))
            .ShouldHaveValidationErrorFor(x => x.FileSizeBytes);

    [Fact]
    public void NullFileStream_Fails()
        => _sut.TestValidate(new UploadDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "doc.pdf", "application/pdf", 1024, null!))
            .ShouldHaveValidationErrorFor(x => x.FileStream);
}

#endregion

#region UpdateJournalEntryValidator

[Trait("Category", "Unit")]
[Trait("Layer", "Validator")]
public class UpdateJournalEntryValidatorTests
{
    private readonly UpdateJournalEntryValidator _sut = new();

    private static UpdateJournalEntryCommand MakeCmd(
        Guid? id = null, Guid? tenantId = null, string desc = "Test yevmiye",
        List<JournalLineInput>? lines = null)
    {
        lines ??= new List<JournalLineInput>
        {
            new(Guid.NewGuid(), 100m, 0m, null),
            new(Guid.NewGuid(), 0m, 100m, null)
        };
        return new UpdateJournalEntryCommand(
            id ?? Guid.NewGuid(), tenantId ?? Guid.NewGuid(), DateTime.UtcNow, desc, null, lines, null);
    }

    [Fact]
    public void Valid_Command_Passes()
        => _sut.TestValidate(MakeCmd()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyDescription_Fails()
        => _sut.TestValidate(MakeCmd(desc: ""))
            .ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void SingleLine_Fails()
        => _sut.TestValidate(MakeCmd(lines: new List<JournalLineInput> { new(Guid.NewGuid(), 100m, 0m, null) }))
            .ShouldHaveValidationErrorFor(x => x.Lines);

    [Fact]
    public void UnbalancedDebitCredit_Fails()
        => _sut.TestValidate(MakeCmd(lines: new List<JournalLineInput>
            {
                new(Guid.NewGuid(), 100m, 0m, null),
                new(Guid.NewGuid(), 0m, 50m, null)
            }))
            .ShouldHaveValidationErrorFor(x => x.Lines);

    [Fact]
    public void EmptyId_Fails()
        => _sut.TestValidate(MakeCmd(id: Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.Id);
}

#endregion
