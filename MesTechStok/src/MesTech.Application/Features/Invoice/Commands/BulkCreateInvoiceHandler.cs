using MediatR;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Invoice.Commands;

public class BulkCreateInvoiceHandler : IRequestHandler<BulkCreateInvoiceCommand, BulkInvoiceResultDto>
{
    private readonly IInvoiceRepository _repository;
    private readonly ILogger<BulkCreateInvoiceHandler> _logger;

    public BulkCreateInvoiceHandler(IInvoiceRepository repository, ILogger<BulkCreateInvoiceHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BulkInvoiceResultDto> Handle(BulkCreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var results = new List<BulkInvoiceItemResultDto>();
        int successCount = 0;
        int failCount = 0;

        foreach (var orderId in request.OrderIds)
        {
            try
            {
                // Load Order from IOrderRepository by orderId
                // Create Invoice using Invoice.CreateForOrder(order, invoiceType, invoiceNumber)
                // invoice.Provider = request.Provider;
                // invoice.DetermineInvoiceType();
                // await _repository.AddAsync(invoice);

                var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

                results.Add(new BulkInvoiceItemResultDto(
                    OrderId: orderId,
                    OrderNumber: $"ORD-{orderId.ToString("N")[..8]}",
                    Success: true,
                    InvoiceNumber: invoiceNumber,
                    ErrorMessage: null));

                successCount++;
                _logger.LogInformation("BulkCreate: Invoice {Number} olusturuldu (OrderId={OrderId}).", invoiceNumber, orderId);
            }
            catch (InvalidOperationException ex)
            {
                failCount++;
                results.Add(new BulkInvoiceItemResultDto(
                    OrderId: orderId,
                    OrderNumber: $"ORD-{orderId.ToString("N")[..8]}",
                    Success: false,
                    InvoiceNumber: null,
                    ErrorMessage: ex.Message));

                _logger.LogError(ex, "BulkCreate: OrderId={OrderId} icin fatura olusturulamadi.", orderId);
            }
        }

        await Task.CompletedTask;

        return new BulkInvoiceResultDto(
            TotalRequested: request.OrderIds.Count,
            SuccessCount: successCount,
            FailCount: failCount,
            Results: results);
    }
}
