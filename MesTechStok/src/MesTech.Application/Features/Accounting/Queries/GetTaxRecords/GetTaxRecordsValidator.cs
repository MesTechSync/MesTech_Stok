using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxRecords;

public sealed class GetTaxRecordsValidator : AbstractValidator<GetTaxRecordsQuery>
{
    public GetTaxRecordsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TaxType).MaximumLength(50)
            .When(x => x.TaxType is not null);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2099)
            .When(x => x.Year.HasValue);
    }
}
