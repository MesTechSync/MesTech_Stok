using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;

public sealed class GetServiceHealthValidator : AbstractValidator<GetServiceHealthQuery>
{
    public GetServiceHealthValidator()
    {
        // No TenantId — infrastructure-level query, no tenant scoping needed.
    }
}
