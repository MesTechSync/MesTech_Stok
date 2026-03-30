using FluentValidation;

namespace MesTech.Application.Queries.GetWarehouseStock;

public sealed class GetWarehouseStockValidator : AbstractValidator<GetWarehouseStockQuery>
{
    public GetWarehouseStockValidator()
    {
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).WithMessage("Geçerli depo ID gerekli.");
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
