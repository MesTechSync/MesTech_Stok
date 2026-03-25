using FluentValidation;

namespace MesTech.Application.Features.EInvoice.Commands;

public sealed class SendEInvoiceValidator : AbstractValidator<SendEInvoiceCommand>
{
    public SendEInvoiceValidator()
    {
        RuleFor(x => x.EInvoiceId).NotEmpty();
    }
}
