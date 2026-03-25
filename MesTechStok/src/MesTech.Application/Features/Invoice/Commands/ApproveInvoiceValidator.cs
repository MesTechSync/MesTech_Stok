using FluentValidation;

namespace MesTech.Application.Features.Invoice.Commands;

public sealed class ApproveInvoiceValidator : AbstractValidator<ApproveInvoiceCommand>
{
    public ApproveInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}
