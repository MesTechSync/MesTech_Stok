using FluentValidation;

namespace MesTech.Application.Commands.DeleteProduct;

public sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
