using FluentValidation;

namespace MesTech.Application.Commands.CreateWarehouse;

public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
        RuleFor(x => x.City).MaximumLength(500).When(x => x.City != null);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
