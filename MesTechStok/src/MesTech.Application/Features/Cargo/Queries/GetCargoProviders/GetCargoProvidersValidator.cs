using FluentValidation;

namespace MesTech.Application.Features.Cargo.Queries.GetCargoProviders;

public sealed class GetCargoProvidersValidator : AbstractValidator<GetCargoProvidersQuery>
{
    public GetCargoProvidersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
