using FluentValidation;

namespace MesTech.Application.Features.Hr.Queries.GetDepartments;

public sealed class GetDepartmentsValidator : AbstractValidator<GetDepartmentsQuery>
{
    public GetDepartmentsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
