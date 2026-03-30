using FluentValidation;

namespace MesTech.Application.Queries.GetWarehouseById;

public sealed class GetWarehouseByIdValidator : AbstractValidator<GetWarehouseByIdQuery>
{
    public GetWarehouseByIdValidator()
    {
        RuleFor(x => x.WarehouseId).NotEqual(Guid.Empty).WithMessage("Geçerli depo ID gerekli.");
    }
}
