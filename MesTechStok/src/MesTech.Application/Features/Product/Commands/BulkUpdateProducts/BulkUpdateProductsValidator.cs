using FluentValidation;

namespace MesTech.Application.Features.Product.Commands.BulkUpdateProducts;

public class BulkUpdateProductsValidator : AbstractValidator<BulkUpdateProductsCommand>
{
    public BulkUpdateProductsValidator()
    {
        // No properties to validate — add rules as business requirements emerge
    }
}
