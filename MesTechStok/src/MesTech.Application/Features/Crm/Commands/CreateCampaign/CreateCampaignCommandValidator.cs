using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.CreateCampaign;

public class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kampanya adı zorunludur.")
            .MaximumLength(200).WithMessage("Kampanya adı en fazla 200 karakter.");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate).WithMessage("Başlangıç tarihi bitiş tarihinden önce olmalı.");

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("İndirim oranı 0-100 arasında olmalı.");
    }
}
