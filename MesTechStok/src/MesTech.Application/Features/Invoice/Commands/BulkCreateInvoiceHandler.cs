using MediatR;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Invoice.Commands;

public sealed class BulkCreateInvoiceHandler : IRequestHandler<BulkCreateInvoiceCommand, BulkInvoiceResultDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkCreateInvoiceHandler> _logger;

    public BulkCreateInvoiceHandler(
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<BulkCreateInvoiceHandler> logger)
    {
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BulkInvoiceResultDto> Handle(BulkCreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var results = new List<BulkInvoiceItemResultDto>();
        int successCount = 0;
        int failCount = 0;

        // Batch fetch — eliminates N+1 query (was: foreach → GetByIdAsync per order)
        var orders = await _orderRepository.GetByIdsAsync(request.OrderIds, cancellationToken).ConfigureAwait(false);
        var orderMap = orders.ToDictionary(o => o.Id);

        foreach (var orderId in request.OrderIds)
        {
            try
            {
                if (!orderMap.TryGetValue(orderId, out var order))
                {
                    failCount++;
                    results.Add(new BulkInvoiceItemResultDto(
                        OrderId: orderId,
                        OrderNumber: string.Empty,
                        Success: false,
                        InvoiceNumber: string.Empty,
                        ErrorMessage: $"Siparis bulunamadi: {orderId}"));
                    continue;
                }

                var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                var invoice = Domain.Entities.Invoice.CreateForOrder(order, InvoiceType.EArsiv, invoiceNumber);
                invoice.Provider = request.Provider;

                await _invoiceRepository.AddAsync(invoice).ConfigureAwait(false);

                results.Add(new BulkInvoiceItemResultDto(
                    OrderId: orderId,
                    OrderNumber: order.OrderNumber ?? $"ORD-{orderId.ToString("N")[..8]}",
                    Success: true,
                    InvoiceNumber: invoiceNumber,
                    ErrorMessage: null));

                successCount++;
                _logger.LogInformation("BulkCreate: Invoice {Number} for Order {OrderNumber} (OrderId={OrderId})",
                    invoiceNumber, order.OrderNumber, orderId);
            }
            catch (Exception ex)
            {
                failCount++;
                results.Add(new BulkInvoiceItemResultDto(
                    OrderId: orderId,
                    OrderNumber: string.Empty,
                    Success: false,
                    InvoiceNumber: string.Empty,
                    ErrorMessage: ex.Message));

                _logger.LogError(ex, "BulkCreate failed for OrderId={OrderId}", orderId);
            }
        }

        if (successCount > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new BulkInvoiceResultDto(
            TotalRequested: request.OrderIds.Count,
            SuccessCount: successCount,
            FailCount: failCount,
            Results: results);
    }
}
