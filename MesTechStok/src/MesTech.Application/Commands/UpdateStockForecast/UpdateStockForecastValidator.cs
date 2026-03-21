using FluentValidation;

namespace MesTech.Application.Commands.UpdateStockForecast;

public class UpdateStockForecastValidator : AbstractValidator<UpdateStockForecastCommand>
{
    public UpdateStockForecastValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
