using FluentValidation;

namespace MesTech.Application.Features.Stock.Commands.StartStockCount;

public sealed class StartStockCountValidator : AbstractValidator<StartStockCountCommand>
{
    public StartStockCountValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}
