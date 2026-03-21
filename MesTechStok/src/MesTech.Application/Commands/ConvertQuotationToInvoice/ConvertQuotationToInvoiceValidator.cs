using FluentValidation;

namespace MesTech.Application.Commands.ConvertQuotationToInvoice;

public class ConvertQuotationToInvoiceValidator : AbstractValidator<ConvertQuotationToInvoiceCommand>
{
    public ConvertQuotationToInvoiceValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty();
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(500);
    }
}
