using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.EarnPoints;

public sealed class EarnPointsCommandValidator : AbstractValidator<EarnPointsCommand>
{
    public EarnPointsCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");

        RuleFor(x => x.CustomerId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli müşteri ID gerekli.");

        RuleFor(x => x.OrderId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli sipariş ID gerekli.");

        RuleFor(x => x.OrderAmount)
            .GreaterThan(0).WithMessage("Sipariş tutarı pozitif olmalı.");
    }
}
