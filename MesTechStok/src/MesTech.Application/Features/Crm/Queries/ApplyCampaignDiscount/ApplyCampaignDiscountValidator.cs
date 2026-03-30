using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;

public sealed class ApplyCampaignDiscountValidator : AbstractValidator<ApplyCampaignDiscountQuery>
{
    public ApplyCampaignDiscountValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEqual(Guid.Empty)
            .WithMessage("Ürün kimliği boş olamaz.");
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
