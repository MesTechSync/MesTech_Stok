using MediatR;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// OrderReceivedEvent -> otomatik Income kaydi olusturma.
/// Platform siparisi alindiginda OnMuhasebe modulu icin gelir kaydi yaratir.
/// CQRS: CreateIncomeCommand dispatch eder, inline entity creation YASAK.
/// </summary>
public sealed class OrderReceivedIncomeHandler
    : INotificationHandler<DomainEventNotification<OrderReceivedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderReceivedIncomeHandler> _logger;

    public OrderReceivedIncomeHandler(
        IMediator mediator,
        ILogger<OrderReceivedIncomeHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderReceivedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        _logger.LogInformation(
            "OrderReceived -> Income: Dispatching CreateIncomeCommand for order {OrderId}, platform={Platform}, amount={Amount}",
            e.OrderId, e.PlatformCode, e.TotalAmount);

        var command = new CreateIncomeCommand(
            TenantId: e.TenantId,
            StoreId: null,
            Description: $"Satis geliri — {e.PlatformCode} #{e.PlatformOrderId}",
            Amount: e.TotalAmount,
            IncomeType: IncomeType.Satis,
            InvoiceId: null,
            Date: e.OccurredAt,
            Note: $"Otomatik olusturuldu. OrderId: {e.OrderId}");

        try
        {
            var incomeId = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "OrderReceived -> Income: Income record {IncomeId} created for order {OrderId}",
                incomeId, e.OrderId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "OrderReceived -> Income FAILED for order {OrderId}. " +
                "Income record will need manual creation. Order processing continues.",
                e.OrderId);
        }
    }
}
