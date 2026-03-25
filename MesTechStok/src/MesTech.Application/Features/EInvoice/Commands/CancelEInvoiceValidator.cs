using FluentValidation;

namespace MesTech.Application.Features.EInvoice.Commands;

public sealed class CancelEInvoiceValidator : AbstractValidator<CancelEInvoiceCommand>
{
    public CancelEInvoiceValidator()
    {
        RuleFor(x => x.EInvoiceId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
