using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.ProcessBotInvoiceRequest;

public record ProcessBotInvoiceRequestCommand : IRequest
{
    public string CustomerPhone { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string RequestChannel { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
}

public sealed class ProcessBotInvoiceRequestHandler : IRequestHandler<ProcessBotInvoiceRequestCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<ProcessBotInvoiceRequestHandler> _logger;

    public ProcessBotInvoiceRequestHandler(
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        ILogger<ProcessBotInvoiceRequestHandler> logger)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    public async Task Handle(ProcessBotInvoiceRequestCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(request.OrderNumber).ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogWarning("ProcessBotInvoiceRequest: Order not found — OrderNumber={OrderNumber}", request.OrderNumber);
            return;
        }

        var invoice = await _invoiceRepository.GetByOrderIdAsync(order.Id).ConfigureAwait(false);
        if (invoice is not null)
        {
            _logger.LogInformation(
                "ProcessBotInvoiceRequest: Invoice found — OrderNumber={OrderNumber}, InvoiceId={InvoiceId}, PdfUrl={PdfUrl}",
                request.OrderNumber, invoice.Id, invoice.PdfUrl);
            // Future: Send PdfUrl via WhatsApp using IMesaBotService
        }
        else
        {
            _logger.LogInformation(
                "ProcessBotInvoiceRequest: No invoice yet — OrderNumber={OrderNumber}", request.OrderNumber);
            // Future: Send "faturaniz henuz hazirlanmadi" message
        }
    }
}
