using FluentValidation;

namespace MesTech.Application.Commands.CreateEInvoiceFromDraft;

public sealed class CreateEInvoiceFromDraftValidator : AbstractValidator<CreateEInvoiceFromDraftCommand>
{
    public CreateEInvoiceFromDraftValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.SuggestedEttnNo).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
