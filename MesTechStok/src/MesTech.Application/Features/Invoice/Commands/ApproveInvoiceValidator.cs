using FluentValidation;

namespace MesTech.Application.Features.Invoice.Commands;

public class ApproveInvoiceValidator : AbstractValidator<ApproveInvoiceCommand>
{
    public ApproveInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}
