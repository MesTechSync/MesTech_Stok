using FluentValidation;

namespace MesTech.Application.Features.Tenant.Commands.CreateTenant;

public sealed class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(20);
    }
}
