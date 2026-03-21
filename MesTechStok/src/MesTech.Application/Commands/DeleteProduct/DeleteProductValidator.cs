using FluentValidation;

namespace MesTech.Application.Commands.DeleteProduct;

public class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
