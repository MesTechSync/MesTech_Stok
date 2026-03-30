using FluentValidation;

namespace MesTech.Application.Features.Reporting.Queries.GetSavedReports;

public sealed class GetSavedReportsValidator : AbstractValidator<GetSavedReportsQuery>
{
    public GetSavedReportsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
