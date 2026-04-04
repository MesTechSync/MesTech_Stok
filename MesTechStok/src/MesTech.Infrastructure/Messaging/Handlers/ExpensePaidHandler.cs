using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Finance;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// ExpensePaidEvent → GL muhasebe kaydı oluşturur.
/// Gider ödendiğinde otomatik GL Transaction yaratır.
/// DEV3 H28 Task 3.2
/// </summary>
public sealed class ExpensePaidHandler : INotificationHandler<DomainEventNotification<ExpensePaidEvent>>
{
    private readonly IExpenseRepository _expenseRepo;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ExpensePaidHandler> _logger;

    public ExpensePaidHandler(
        IExpenseRepository expenseRepo,
        IDbContextFactory<AppDbContext> contextFactory,
        ITenantProvider tenantProvider,
        ILogger<ExpensePaidHandler> logger)
    {
        _expenseRepo = expenseRepo;
        _contextFactory = contextFactory;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<ExpensePaidEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var expense = await _expenseRepo.GetByIdAsync(e.ExpenseId, cancellationToken).ConfigureAwait(false);
        if (expense is null)
        {
            _logger.LogWarning("ExpensePaid: Expense {ExpenseId} not found in DB", e.ExpenseId);
            return;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId() != Guid.Empty
            ? _tenantProvider.GetCurrentTenantId()
            : expense.TenantId;

        var glEntry = GLTransaction.Create(
            tenantId: tenantId,
            type: GLTransactionType.Expense,
            amount: expense.Amount,
            description: $"Gider ödeme: {expense.Description}",
            createdByUserId: tenantId, // system
            bankAccountId: e.BankAccountId,
            expenseId: e.ExpenseId);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await context.Set<GLTransaction>().AddAsync(glEntry, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "ExpensePaid GL kaydı oluşturuldu: ExpenseId={ExpenseId} Amount={Amount} TenantId={TenantId}",
            e.ExpenseId, expense.Amount, tenantId);
    }
}
