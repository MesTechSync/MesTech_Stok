using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Orders.Commands.ExportOrders;

public sealed class ExportOrdersHandler : IRequestHandler<ExportOrdersCommand, ExportOrdersResult>
{
    private readonly IOrderRepository _orderRepo;
    private readonly IExcelExportService _excelService;

    public ExportOrdersHandler(IOrderRepository orderRepo, IExcelExportService excelService)
    {
        _orderRepo = orderRepo;
        _excelService = excelService;
    }

    public async Task<ExportOrdersResult> Handle(ExportOrdersCommand request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepo.GetByDateRangeAsync(request.TenantId, request.From, request.To, cancellationToken);

        var filtered = request.PlatformFilter is not null
            ? orders.Where(o => string.Equals(o.SourcePlatform?.ToString(), request.PlatformFilter, StringComparison.OrdinalIgnoreCase)).ToList()
            : orders.ToList();

        if (filtered.Count == 0)
            return new ExportOrdersResult { IsSuccess = false, ErrorMessage = "Dışa aktarılacak sipariş bulunamadı." };

        var exportDtos = filtered.Select(o => new OrderExportDto(
            o.OrderNumber,
            o.CustomerName ?? string.Empty,
            o.OrderDate,
            o.TotalAmount,
            o.Status.ToString(),
            o.TrackingNumber
        )).ToList();

        using var stream = await _excelService.ExportOrdersAsync(exportDtos, cancellationToken);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);

        return new ExportOrdersResult
        {
            IsSuccess = true,
            FileContent = ms.ToArray(),
            FileName = $"siparisler_{request.From:yyyyMMdd}_{request.To:yyyyMMdd}.xlsx",
            ExportedCount = filtered.Count
        };
    }
}
