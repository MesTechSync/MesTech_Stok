using FluentValidation;

namespace MesTech.Application.Features.Tenant.Queries.GetTenant;

public sealed class GetTenantValidator : AbstractValidator<GetTenantQuery>
{
    public GetTenantValidator() { RuleFor(x => x.TenantId).NotEmpty(); }
}
