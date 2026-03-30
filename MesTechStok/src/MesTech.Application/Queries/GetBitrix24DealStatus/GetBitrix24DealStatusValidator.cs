using FluentValidation;

namespace MesTech.Application.Queries.GetBitrix24DealStatus;

public sealed class GetBitrix24DealStatusValidator : AbstractValidator<GetBitrix24DealStatusQuery>
{
    public GetBitrix24DealStatusValidator()
    {
        RuleFor(x => x.OrderId).NotEqual(Guid.Empty).WithMessage("Geçerli sipariş ID gerekli.");
    }
}
