using FluentValidation;

namespace MesTech.Application.Features.Reports.OrderFulfillmentReport;

public sealed class OrderFulfillmentReportValidator : AbstractValidator<OrderFulfillmentReportQuery>
{
    public OrderFulfillmentReportValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("Bitiş tarihi başlangıçtan sonra olmalı.");
    }
}
