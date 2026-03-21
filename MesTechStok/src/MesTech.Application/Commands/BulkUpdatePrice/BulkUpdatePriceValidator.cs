using FluentValidation;

namespace MesTech.Application.Commands.BulkUpdatePrice;

public class BulkUpdatePriceValidator : AbstractValidator<BulkUpdatePriceCommand>
{
    public BulkUpdatePriceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
