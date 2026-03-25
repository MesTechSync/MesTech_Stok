using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Masraf onaylandığında loglama ve ödeme sürecini tetikleme.
/// Infrastructure MediatR bridge handler bu servisi çağırır.
/// </summary>
public interface IExpenseApprovedEventHandler
{
    Task HandleAsync(Guid expenseId, Guid approvedByUserId, CancellationToken ct);
}

public sealed class ExpenseApprovedEventHandler : IExpenseApprovedEventHandler
{
    private readonly ILogger<ExpenseApprovedEventHandler> _logger;

    public ExpenseApprovedEventHandler(ILogger<ExpenseApprovedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(Guid expenseId, Guid approvedByUserId, CancellationToken ct)
    {
        _logger.LogInformation(
            "ExpenseApproved → ExpenseId={ExpenseId}, ApprovedBy={ApprovedByUserId}",
            expenseId, approvedByUserId);

        // FUTURE: Ödeme işleme sürecini tetikle
        // FUTURE: ERP'ye masraf kaydı push et (Parasut)

        return Task.CompletedTask;
    }
}
