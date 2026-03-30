using FluentValidation;

namespace MesTech.Application.Queries.GetLowStockProducts;

public sealed class GetLowStockProductsValidator : AbstractValidator<GetLowStockProductsQuery>
{
    public GetLowStockProductsValidator() { }
}
