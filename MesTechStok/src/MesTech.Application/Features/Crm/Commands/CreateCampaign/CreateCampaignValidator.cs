using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.CreateCampaign;

public class CreateCampaignValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0.01m, 100m);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
