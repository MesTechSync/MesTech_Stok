using FluentValidation;

namespace MesTech.Application.Commands.AcceptQuotation;

public sealed class AcceptQuotationValidator : AbstractValidator<AcceptQuotationCommand>
{
    public AcceptQuotationValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty();
    }
}
