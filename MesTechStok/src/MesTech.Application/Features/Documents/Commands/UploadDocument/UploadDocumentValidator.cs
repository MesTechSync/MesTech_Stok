using FluentValidation;

namespace MesTech.Application.Features.Documents.Commands.UploadDocument;

public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf", "image/jpeg", "image/png", "image/gif",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/csv", "text/plain", "application/xml", "application/json"
    ];

    public UploadDocumentValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId zorunlu.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId zorunlu.");
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255).WithMessage("Dosya adi 255 karakter limit.");
        RuleFor(x => x.ContentType).NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Desteklenmeyen dosya tipi.");
        RuleFor(x => x.FileSizeBytes).GreaterThan(0).WithMessage("Dosya boyutu sifirdan buyuk olmali.");
        RuleFor(x => x.FileStream).NotNull().WithMessage("Dosya stream bos olamaz.");
    }
}
