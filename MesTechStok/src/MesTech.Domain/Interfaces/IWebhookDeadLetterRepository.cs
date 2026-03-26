using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IWebhookDeadLetterRepository
{
    Task AddAsync(WebhookDeadLetter item, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookDeadLetter>> GetPendingRetryAsync(DateTime asOfDate, CancellationToken ct = default);
    Task<(IReadOnlyList<WebhookDeadLetter> Items, int TotalCount)> GetPagedAsync(
        WebhookDeadLetterStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<WebhookDeadLetter?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
