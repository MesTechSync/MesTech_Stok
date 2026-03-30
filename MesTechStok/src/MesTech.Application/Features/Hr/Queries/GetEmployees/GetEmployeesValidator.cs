using FluentValidation;

namespace MesTech.Application.Features.Hr.Queries.GetEmployees;

public sealed class GetEmployeesValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
