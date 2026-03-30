using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;

public sealed class GetTaxRecordByIdValidator : AbstractValidator<GetTaxRecordByIdQuery>
{
    public GetTaxRecordByIdValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
