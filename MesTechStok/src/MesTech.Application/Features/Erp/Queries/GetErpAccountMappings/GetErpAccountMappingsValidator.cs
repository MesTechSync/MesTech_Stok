using FluentValidation;

namespace MesTech.Application.Features.Erp.Queries.GetErpAccountMappings;

public sealed class GetErpAccountMappingsValidator : AbstractValidator<GetErpAccountMappingsQuery>
{
    public GetErpAccountMappingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
