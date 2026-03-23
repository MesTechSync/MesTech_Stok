using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class UploadAccountingDocumentValidatorTests
{
    private readonly UploadAccountingDocumentValidator _validator = new();

    private static UploadAccountingDocumentCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        FileName: "fatura_2026_03.pdf",
        MimeType: "application/pdf",
        FileSize: 1_048_576,
        StoragePath: "/documents/2026/03/fatura_2026_03.pdf",
        DocumentType: DocumentType.Invoice,
        DocumentSource: DocumentSource.Upload,
        CounterpartyId: Guid.NewGuid(),
        Amount: 15_000m,
        ExtractedData: "{\"total\": 15000}"
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
    public async Task EmptyFileName_FailsValidation()
    {
        var cmd = ValidCommand() with { FileName = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task FileNameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { FileName = new string('F', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileName");
    }

    [Fact]
    public async Task EmptyMimeType_FailsValidation()
    {
        var cmd = ValidCommand() with { MimeType = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MimeType");
    }

    [Fact]
    public async Task MimeTypeTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { MimeType = new string('M', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MimeType");
    }

    [Fact]
    public async Task EmptyStoragePath_FailsValidation()
    {
        var cmd = ValidCommand() with { StoragePath = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoragePath");
    }

    [Fact]
    public async Task StoragePathTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { StoragePath = new string('S', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoragePath");
    }

    [Fact]
    public async Task InvalidDocumentType_FailsValidation()
    {
        var cmd = ValidCommand() with { DocumentType = (DocumentType)200 };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentType");
    }

    [Theory]
    [InlineData(DocumentType.Invoice)]
    [InlineData(DocumentType.Receipt)]
    [InlineData(DocumentType.BankStatement)]
    [InlineData(DocumentType.Settlement)]
    public async Task ValidDocumentType_PassesValidation(DocumentType type)
    {
        var cmd = ValidCommand() with { DocumentType = type };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractedDataTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { ExtractedData = new string('E', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExtractedData");
    }

    [Fact]
    public async Task NullExtractedData_PassesValidation()
    {
        var cmd = ValidCommand() with { ExtractedData = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
