using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.GenerateEFatura;

public record GenerateEFaturaCommand : IRequest
{
    public string BotUserId { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public string? BuyerVkn { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class GenerateEFaturaHandler : IRequestHandler<GenerateEFaturaCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateEFaturaHandler> _logger;

    public GenerateEFaturaHandler(
        IOrderRepository orderRepository,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<GenerateEFaturaHandler> logger)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(GenerateEFaturaCommand request, CancellationToken cancellationToken)
    {
        if (request.OrderId is null)
        {
            _logger.LogWarning("GenerateEFatura: OrderId is null, skipping");
            return;
        }

        var order = await _orderRepository.GetByIdAsync(request.OrderId.Value, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogWarning("GenerateEFatura: Order {OrderId} not found", request.OrderId);
            return;
        }

        var invoiceType = !string.IsNullOrEmpty(request.BuyerVkn) ? InvoiceType.EFatura : InvoiceType.EArsiv;
        var invoiceNumber = $"EF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var invoice = Invoice.CreateForOrder(order, invoiceType, invoiceNumber);
        await _invoiceRepository.AddAsync(invoice).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("GenerateEFatura: {InvoiceNumber} ({Type}) created for Order {OrderId}",
            invoiceNumber, invoiceType, request.OrderId);
    }
}
