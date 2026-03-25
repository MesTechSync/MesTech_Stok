using FluentValidation;

namespace MesTech.Application.Commands.BulkUpdateStock;

public sealed class BulkUpdateStockValidator : AbstractValidator<BulkUpdateStockCommand>
{
    public BulkUpdateStockValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
