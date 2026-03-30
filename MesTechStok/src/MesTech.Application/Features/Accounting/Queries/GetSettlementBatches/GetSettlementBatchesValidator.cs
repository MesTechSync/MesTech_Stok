using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;

public sealed class GetSettlementBatchesValidator : AbstractValidator<GetSettlementBatchesQuery>
{
    public GetSettlementBatchesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Platform).MaximumLength(50)
            .When(x => x.Platform is not null);
    }
}
