using FluentValidation;

namespace MesTech.Application.Commands.DeleteSupplier;

public sealed class DeleteSupplierValidator : AbstractValidator<DeleteSupplierCommand>
{
    public DeleteSupplierValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
    }
}
