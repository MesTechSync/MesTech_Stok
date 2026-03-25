using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// InvoiceCreatedEvent -> otomatik Expense kaydi olusturma.
/// Fatura olusturuldugunda OnMuhasebe modulu icin gider kaydi yaratir.
/// Komisyon ve kargo giderleri fatura tutarindan otomatik hesaplanir.
/// </summary>
public sealed class InvoiceGeneratedExpenseHandler
    : INotificationHandler<DomainEventNotification<InvoiceCreatedEvent>>
{
    private readonly IExpenseRepository _expenseRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InvoiceGeneratedExpenseHandler> _logger;

    public InvoiceGeneratedExpenseHandler(
        IExpenseRepository expenseRepo,
        IUnitOfWork uow,
        ILogger<InvoiceGeneratedExpenseHandler> logger)
    {
        _expenseRepo = expenseRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        _logger.LogInformation(
            "InvoiceCreated -> Expense: Creating expense record for invoice {InvoiceId}, order={OrderId}, amount={Amount}",
            e.InvoiceId, e.OrderId, e.GrandTotal);

        var expense = new Expense
        {
            TenantId = e.TenantId,
            Description = $"Fatura gideri — Fatura #{e.InvoiceId:N} (Tip: {e.Type})",
            ExpenseType = ExpenseType.Diger,
            Date = e.OccurredAt,
            Note = $"Otomatik olusturuldu. InvoiceId: {e.InvoiceId}, OrderId: {e.OrderId}"
        };
        expense.SetAmount(e.GrandTotal);

        await _expenseRepo.AddAsync(expense).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "InvoiceCreated -> Expense: Expense record {ExpenseId} created for invoice {InvoiceId}",
            expense.Id, e.InvoiceId);
    }
}
