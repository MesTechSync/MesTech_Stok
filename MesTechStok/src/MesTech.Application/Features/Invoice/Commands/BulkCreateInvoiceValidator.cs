using FluentValidation;

namespace MesTech.Application.Features.Invoice.Commands;

public sealed class BulkCreateInvoiceValidator : AbstractValidator<BulkCreateInvoiceCommand>
{
    public BulkCreateInvoiceValidator()
    {
        RuleFor(x => x.OrderIds)
            .NotEmpty().WithMessage("En az bir sipariş seçilmelidir.")
            .Must(ids => ids.Count <= 100).WithMessage("Tek seferde en fazla 100 fatura oluşturulabilir.");

        RuleFor(x => x.Provider)
            .IsInEnum().WithMessage("Geçersiz fatura sağlayıcısı.");
    }
}
