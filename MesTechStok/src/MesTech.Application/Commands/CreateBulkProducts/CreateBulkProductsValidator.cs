using FluentValidation;

namespace MesTech.Application.Commands.CreateBulkProducts;

public class CreateBulkProductsValidator : AbstractValidator<CreateBulkProductsCommand>
{
    public CreateBulkProductsValidator()
    {
        RuleFor(x => x.Count).GreaterThanOrEqualTo(0);
    }
}
