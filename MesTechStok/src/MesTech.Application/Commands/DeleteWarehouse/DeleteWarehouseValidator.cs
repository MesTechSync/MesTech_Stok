using FluentValidation;

namespace MesTech.Application.Commands.DeleteWarehouse;

public sealed class DeleteWarehouseValidator : AbstractValidator<DeleteWarehouseCommand>
{
    public DeleteWarehouseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}
