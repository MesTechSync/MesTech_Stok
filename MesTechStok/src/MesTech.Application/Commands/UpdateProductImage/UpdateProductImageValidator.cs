using FluentValidation;

namespace MesTech.Application.Commands.UpdateProductImage;

public sealed class UpdateProductImageValidator : AbstractValidator<UpdateProductImageCommand>
{
    public UpdateProductImageValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
    }
}
