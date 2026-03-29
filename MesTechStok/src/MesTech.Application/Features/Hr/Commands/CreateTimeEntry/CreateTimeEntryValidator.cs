using FluentValidation;

namespace MesTech.Application.Features.Hr.Commands.CreateTimeEntry;

public sealed class CreateTimeEntryValidator : AbstractValidator<CreateTimeEntryCommand>
{
    public CreateTimeEntryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");

        RuleFor(x => x.WorkTaskId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli görev ID gerekli.");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli kullanıcı ID gerekli.");

        RuleFor(x => x.HourlyRate)
            .GreaterThan(0).When(x => x.HourlyRate.HasValue)
            .WithMessage("Saatlik ücret pozitif olmalı.");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null)
            .WithMessage("Açıklama en fazla 500 karakter olabilir.");
    }
}
