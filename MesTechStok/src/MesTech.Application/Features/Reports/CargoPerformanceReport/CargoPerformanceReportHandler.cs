#pragma warning disable MA0051 // Method is too long — report handler is a single cohesive operation
#pragma warning disable NX0003 // NullForgiving justified — null filtered by Where/HasValue clause
using MediatR;
using MesTech.Application.DTOs.Reports;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reports.CargoPerformanceReport;

/// <summary>
/// Kargo performans raporu handler'i.
/// CargoExpense + Order (shipment/delivery) verilerini kargo saglayici bazinda gruplar.
/// </summary>
public sealed class CargoPerformanceReportHandler
    : IRequestHandler<CargoPerformanceReportQuery, IReadOnlyList<CargoPerformanceReportDto>>
{
    private readonly ICargoExpenseRepository _cargoExpenseRepository;
    private readonly IOrderRepository _orderRepository;

    public CargoPerformanceReportHandler(
        ICargoExpenseRepository cargoExpenseRepository,
        IOrderRepository orderRepository)
    {
        _cargoExpenseRepository = cargoExpenseRepository;
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<CargoPerformanceReportDto>> Handle(
        CargoPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cargoExpenses = await _cargoExpenseRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        var orders = await _orderRepository.GetByDateRangeAsync(
            request.TenantId, request.StartDate, request.EndDate, cancellationToken);

        // Group shipped orders by cargo provider
        var shippedOrders = orders
            .Where(o => o.CargoProvider.HasValue && o.CargoProvider.Value != CargoProvider.None)
            .GroupBy(o => o.CargoProvider!.Value) // NX0003: Null filtered by Where clause above
            .ToDictionary(g => g.Key, g => g.ToList());

        // Group cargo expenses by carrier name
        var expensesByCarrier = cargoExpenses
            .GroupBy(e => e.CarrierName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var result = new List<CargoPerformanceReportDto>();

        foreach (var provider in Enum.GetValues<CargoProvider>())
        {
            if (provider == CargoProvider.None)
                continue;

            var providerName = provider.ToString();

            if (!shippedOrders.TryGetValue(provider, out var providerOrders))
                continue;

            var shipmentCount = providerOrders.Count;

            // Average delivery days: DeliveredAt - ShippedAt
            var deliveredOrders = providerOrders
                .Where(o => o.DeliveredAt.HasValue && o.ShippedAt.HasValue)
                .ToList();

            var avgDeliveryDays = deliveredOrders.Count > 0
                ? deliveredOrders.Average(o => (o.DeliveredAt!.Value - o.ShippedAt!.Value).TotalDays) // NX0003: Null filtered by Where clause above
                : 0.0;

            // Average cost from cargo expenses
            var avgCost = expensesByCarrier.TryGetValue(providerName, out var expenses) && expenses.Count > 0
                ? expenses.Average(e => e.Cost)
                : 0m;

            // Success rate: delivered / total shipped
            var successRate = shipmentCount > 0
                ? (double)deliveredOrders.Count / shipmentCount * 100.0
                : 0.0;

            result.Add(new CargoPerformanceReportDto
            {
                CargoProvider = providerName,
                ShipmentCount = shipmentCount,
                AvgDeliveryDays = Math.Round(avgDeliveryDays, 1),
                AvgCost = Math.Round(avgCost, 2),
                SuccessRate = Math.Round(successRate, 1)
            });
        }

        return result
            .OrderByDescending(r => r.ShipmentCount)
            .ToList()
            .AsReadOnly();
    }
}
