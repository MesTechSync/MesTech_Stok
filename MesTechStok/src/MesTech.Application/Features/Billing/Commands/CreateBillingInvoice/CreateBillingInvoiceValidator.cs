using FluentValidation;

namespace MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;

public sealed class CreateBillingInvoiceValidator : AbstractValidator<CreateBillingInvoiceCommand>
{
    public CreateBillingInvoiceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Fatura tutari pozitif olmali.");
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.TaxRate).InclusiveBetween(0, 1);
        RuleFor(x => x.DueDays).GreaterThan(0);
    }
}
