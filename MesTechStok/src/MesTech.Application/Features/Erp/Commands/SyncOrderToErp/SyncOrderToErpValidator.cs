using FluentValidation;

namespace MesTech.Application.Features.Erp.Commands.SyncOrderToErp;

public class SyncOrderToErpValidator : AbstractValidator<SyncOrderToErpCommand>
{
    public SyncOrderToErpValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
