using MediatR;
using MesTech.Application.DTOs.Fulfillment;

namespace MesTech.Application.Features.Reports.FulfillmentCostReport;

public record FulfillmentCostReportQuery(
    Guid TenantId,
    DateTime StartDate,
    DateTime EndDate,
    FulfillmentCenter? CenterFilter = null
) : IRequest<FulfillmentCostReportDto>;

public record FulfillmentCostReportDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalFulfillmentCost { get; init; }
    public IReadOnlyList<CenterCostDto> Centers { get; init; } = [];
}

public record CenterCostDto(
    FulfillmentCenter Center,
    string CenterName,
    int TotalItems,
    decimal InventoryValue,
    decimal FulfillmentFee,
    decimal AverageCostPerItem,
    bool IsAvailable);
