using FluentValidation;

namespace MesTech.Application.Commands.RejectQuotation;

public class RejectQuotationValidator : AbstractValidator<RejectQuotationCommand>
{
    public RejectQuotationValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty();
    }
}
