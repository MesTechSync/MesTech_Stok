using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries.GetSupplierPerformance;

public sealed class GetSupplierPerformanceValidator : AbstractValidator<GetSupplierPerformanceQuery>
{
    public GetSupplierPerformanceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
