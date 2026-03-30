using FluentValidation;

namespace MesTech.Application.Queries.GetQuotationById;

public sealed class GetQuotationByIdValidator : AbstractValidator<GetQuotationByIdQuery>
{
    public GetQuotationByIdValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).WithMessage("Geçerli teklif ID gerekli.");
    }
}
