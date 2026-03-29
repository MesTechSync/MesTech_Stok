using FluentValidation;

namespace MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;

public sealed class DeleteErpAccountMappingValidator : AbstractValidator<DeleteErpAccountMappingCommand>
{
    public DeleteErpAccountMappingValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");

        RuleFor(x => x.MappingId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli mapping ID gerekli.");
    }
}
