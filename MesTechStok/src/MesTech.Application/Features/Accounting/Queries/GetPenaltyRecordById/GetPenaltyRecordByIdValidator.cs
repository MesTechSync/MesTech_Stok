using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;

public sealed class GetPenaltyRecordByIdValidator : AbstractValidator<GetPenaltyRecordByIdQuery>
{
    public GetPenaltyRecordByIdValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
