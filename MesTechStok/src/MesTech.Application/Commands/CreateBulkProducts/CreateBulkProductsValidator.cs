using FluentValidation;

namespace MesTech.Application.Commands.CreateBulkProducts;

public sealed class CreateBulkProductsValidator : AbstractValidator<CreateBulkProductsCommand>
{
    public CreateBulkProductsValidator()
    {
        RuleFor(x => x.Count).GreaterThanOrEqualTo(0);
    }
}
