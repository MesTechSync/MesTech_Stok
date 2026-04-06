using FluentValidation;

namespace MesTech.Application.Queries.GetSupplierById;

public sealed class GetSupplierByIdValidator : AbstractValidator<GetSupplierByIdQuery>
{
    public GetSupplierByIdValidator()
    {
        RuleFor(x => x.SupplierId)
            .NotEmpty()
            .WithMessage("SupplierId is required.");
    }
}
