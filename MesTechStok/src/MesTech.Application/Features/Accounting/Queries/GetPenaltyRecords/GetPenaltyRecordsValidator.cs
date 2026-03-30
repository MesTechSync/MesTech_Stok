using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;

public sealed class GetPenaltyRecordsValidator : AbstractValidator<GetPenaltyRecordsQuery>
{
    public GetPenaltyRecordsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
