using FluentValidation;

namespace MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;

public sealed class DeletePersonalDataValidator : AbstractValidator<DeletePersonalDataCommand>
{
    public DeletePersonalDataValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Silme sebebi belirtilmeli (KVKK zorunlulugu).");
    }
}
