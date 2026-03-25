using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.CreateLead;

public sealed class CreateLeadValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Email).MaximumLength(500).When(x => x.Email != null);
        RuleFor(x => x.Phone).MaximumLength(500).When(x => x.Phone != null);
        RuleFor(x => x.Company).MaximumLength(500).When(x => x.Company != null);
    }
}
