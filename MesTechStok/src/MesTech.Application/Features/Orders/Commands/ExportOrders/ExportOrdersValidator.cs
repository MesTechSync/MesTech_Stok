using FluentValidation;

namespace MesTech.Application.Features.Orders.Commands.ExportOrders;

public sealed class ExportOrdersValidator : AbstractValidator<ExportOrdersCommand>
{
    public ExportOrdersValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId zorunlu.");

        RuleFor(x => x.From)
            .LessThan(x => x.To).WithMessage("Başlangıç tarihi bitiş tarihinden önce olmalı.");

        RuleFor(x => x.To)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Bitiş tarihi gelecekte olamaz.");
    }
}
