using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UploadAccountingDocument;

public class UploadAccountingDocumentValidator : AbstractValidator<UploadAccountingDocumentCommand>
{
    public UploadAccountingDocumentValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(500);
        RuleFor(x => x.StoragePath).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.ExtractedData).MaximumLength(500).When(x => x.ExtractedData != null);
    }
}
