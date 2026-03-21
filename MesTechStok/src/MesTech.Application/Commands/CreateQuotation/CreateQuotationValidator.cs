using FluentValidation;

namespace MesTech.Application.Commands.CreateQuotation;

public class CreateQuotationValidator : AbstractValidator<CreateQuotationCommand>
{
    public CreateQuotationValidator()
    {
        RuleFor(x => x.QuotationNumber).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CustomerTaxNumber).MaximumLength(500).When(x => x.CustomerTaxNumber != null);
        RuleFor(x => x.CustomerTaxOffice).MaximumLength(500).When(x => x.CustomerTaxOffice != null);
        RuleFor(x => x.CustomerAddress).MaximumLength(500).When(x => x.CustomerAddress != null);
        RuleFor(x => x.CustomerEmail).MaximumLength(500).When(x => x.CustomerEmail != null);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes != null);
        RuleFor(x => x.Terms).MaximumLength(500).When(x => x.Terms != null);
    }
}
