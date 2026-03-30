using FluentValidation;

namespace MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;

public sealed class ExportPersonalDataValidator : AbstractValidator<ExportPersonalDataQuery>
{
    public ExportPersonalDataValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEqual(Guid.Empty)
            .WithMessage("Talep eden kullanici kimlik bilgisi bos olamaz.");
    }
}
