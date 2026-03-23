using FluentValidation;

namespace MesTech.Application.Features.Tenant.Queries.GetTenant;

public class GetTenantValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
