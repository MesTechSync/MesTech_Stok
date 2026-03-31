using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Gider yaşam döngüsü bildirimleri — Approved/Paid.
/// ExpenseApprovedEvent, ExpensePaidEvent → NotificationLog.
/// </summary>
public interface IExpenseNotificationHandler
{
    Task HandleApprovedAsync(Guid expenseId, Guid tenantId, Guid approvedByUserId, CancellationToken ct);
    Task HandlePaidAsync(Guid expenseId, Guid tenantId, Guid bankAccountId, CancellationToken ct);
}

public sealed class ExpenseNotificationHandler : IExpenseNotificationHandler
{
    private readonly INotificationLogRepository _notificationRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExpenseNotificationHandler> _logger;

    public ExpenseNotificationHandler(
        INotificationLogRepository notificationRepo,
        IUnitOfWork uow,
        ILogger<ExpenseNotificationHandler> logger)
    {
        _notificationRepo = notificationRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleApprovedAsync(Guid expenseId, Guid tenantId, Guid approvedByUserId, CancellationToken ct)
    {
        _logger.LogInformation("ExpenseApproved → bildirim. ExpenseId={Id}, ApprovedBy={User}", expenseId, approvedByUserId);
        await CreateNotificationAsync(tenantId, "ExpenseApproved",
            $"Gider onaylandı — ID: {expenseId}", ct).ConfigureAwait(false);
    }

    public async Task HandlePaidAsync(Guid expenseId, Guid tenantId, Guid bankAccountId, CancellationToken ct)
    {
        _logger.LogInformation("ExpensePaid → bildirim. ExpenseId={Id}, BankAccount={Bank}", expenseId, bankAccountId);
        await CreateNotificationAsync(tenantId, "ExpensePaid",
            $"Gider ödendi — ID: {expenseId}, Banka Hesabı: {bankAccountId}", ct).ConfigureAwait(false);
    }

    private async Task CreateNotificationAsync(Guid tenantId, string template, string content, CancellationToken ct)
    {
        var notification = NotificationLog.Create(
            tenantId,
            Domain.Enums.NotificationChannel.Push,
            "dashboard",
            template,
            content);

        await _notificationRepo.AddAsync(notification, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
