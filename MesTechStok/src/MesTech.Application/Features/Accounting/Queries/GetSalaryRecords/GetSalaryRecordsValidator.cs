using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;

public sealed class GetSalaryRecordsValidator : AbstractValidator<GetSalaryRecordsQuery>
{
    public GetSalaryRecordsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2099)
            .When(x => x.Year.HasValue);
        RuleFor(x => x.Month).InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue);
    }
}
