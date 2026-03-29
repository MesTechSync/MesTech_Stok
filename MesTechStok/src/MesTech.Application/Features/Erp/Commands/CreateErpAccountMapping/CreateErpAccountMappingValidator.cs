using FluentValidation;

namespace MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;

public sealed class CreateErpAccountMappingValidator : AbstractValidator<CreateErpAccountMappingCommand>
{
    public CreateErpAccountMappingValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.MesTechCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MesTechName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ErpCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ErpName).NotEmpty().MaximumLength(200);
    }
}
