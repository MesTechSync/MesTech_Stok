using MediatR;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// InvoiceCreatedEvent -> otomatik Expense kaydi olusturma.
/// Fatura olusturuldugunda OnMuhasebe modulu icin gider kaydi yaratir.
/// Komisyon ve kargo giderleri fatura tutarindan otomatik hesaplanir.
/// CQRS: CreateExpenseCommand dispatch eder, inline entity creation YASAK.
/// </summary>
public sealed class InvoiceGeneratedExpenseHandler
    : INotificationHandler<DomainEventNotification<InvoiceCreatedEvent>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvoiceGeneratedExpenseHandler> _logger;

    public InvoiceGeneratedExpenseHandler(
        IMediator mediator,
        ILogger<InvoiceGeneratedExpenseHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        _logger.LogInformation(
            "InvoiceCreated -> Expense: Dispatching CreateExpenseCommand for invoice {InvoiceId}, order={OrderId}, amount={Amount}",
            e.InvoiceId, e.OrderId, e.GrandTotal);

        var command = new CreateExpenseCommand(
            TenantId: e.TenantId,
            Title: $"Fatura gideri — Fatura #{e.InvoiceId:N} (Tip: {e.Type})",
            Amount: e.GrandTotal,
            Category: ExpenseCategory.Other,
            ExpenseDate: e.OccurredAt,
            Notes: $"Otomatik olusturuldu. InvoiceId: {e.InvoiceId}, OrderId: {e.OrderId}");

        var expenseId = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "InvoiceCreated -> Expense: Expense record {ExpenseId} created for invoice {InvoiceId}",
            expenseId, e.InvoiceId);
    }
}
