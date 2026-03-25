using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.RedeemPoints;

public sealed class RedeemPointsCommandValidator : AbstractValidator<RedeemPointsCommand>
{
    public RedeemPointsCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");

        RuleFor(x => x.CustomerId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli müşteri ID gerekli.");

        RuleFor(x => x.PointsToRedeem)
            .GreaterThan(0).WithMessage("Kullanılacak puan pozitif olmalı.");
    }
}
