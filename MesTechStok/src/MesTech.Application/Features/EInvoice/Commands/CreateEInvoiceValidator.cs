using FluentValidation;

namespace MesTech.Application.Features.EInvoice.Commands;

public class CreateEInvoiceValidator : AbstractValidator<CreateEInvoiceCommand>
{
    public CreateEInvoiceValidator()
    {
        RuleFor(x => x.BuyerVkn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BuyerTitle).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BuyerEmail).MaximumLength(500).When(x => x.BuyerEmail != null);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.CurrencyCode).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ProviderId).NotEmpty().MaximumLength(500);
    }
}
