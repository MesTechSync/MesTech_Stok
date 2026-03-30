using FluentValidation;

namespace MesTech.Application.Features.Product.Queries.GetBuyboxStatus;

public sealed class GetBuyboxStatusValidator : AbstractValidator<GetBuyboxStatusQuery>
{
    public GetBuyboxStatusValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty)
            .WithMessage("Urun kimlik bilgisi bos olamaz.");
        RuleFor(x => x.PlatformCode).MaximumLength(200)
            .When(x => x.PlatformCode is not null);
    }
}
