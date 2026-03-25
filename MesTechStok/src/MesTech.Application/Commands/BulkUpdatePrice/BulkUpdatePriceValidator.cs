using FluentValidation;

namespace MesTech.Application.Commands.BulkUpdatePrice;

public sealed class BulkUpdatePriceValidator : AbstractValidator<BulkUpdatePriceCommand>
{
    public BulkUpdatePriceValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
