using FluentValidation;

namespace MesTech.Application.Commands.SendInvoice;

public sealed class SendInvoiceValidator : AbstractValidator<SendInvoiceCommand>
{
    public SendInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}
