using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// OrderReceivedEvent -> otomatik Income kaydi olusturma.
/// Platform siparisi alindiginda OnMuhasebe modulu icin gelir kaydi yaratir.
/// </summary>
public class OrderReceivedIncomeHandler
    : INotificationHandler<DomainEventNotification<OrderReceivedEvent>>
{
    private readonly IIncomeRepository _incomeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderReceivedIncomeHandler> _logger;

    public OrderReceivedIncomeHandler(
        IIncomeRepository incomeRepo,
        IUnitOfWork uow,
        ILogger<OrderReceivedIncomeHandler> logger)
    {
        _incomeRepo = incomeRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderReceivedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        _logger.LogInformation(
            "OrderReceived -> Income: Creating income record for order {OrderId}, platform={Platform}, amount={Amount}",
            e.OrderId, e.PlatformCode, e.TotalAmount);

        var income = new Income
        {
            TenantId = Guid.Empty, // Will be resolved by order's TenantId via domain context
            Description = $"Satis geliri — {e.PlatformCode} #{e.PlatformOrderId}",
            Amount = e.TotalAmount,
            IncomeType = IncomeType.Satis,
            Date = e.OccurredAt,
            Note = $"Otomatik olusturuldu. OrderId: {e.OrderId}"
        };

        await _incomeRepo.AddAsync(income);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "OrderReceived -> Income: Income record {IncomeId} created for order {OrderId}",
            income.Id, e.OrderId);
    }
}
