using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class WebhookDeadLetterRepository : IWebhookDeadLetterRepository
{
    private readonly AppDbContext _context;

    public WebhookDeadLetterRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddAsync(WebhookDeadLetter item, CancellationToken ct = default)
        => await _context.Set<WebhookDeadLetter>().AddAsync(item, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<WebhookDeadLetter>> GetPendingRetryAsync(
        DateTime asOfDate, CancellationToken ct = default)
        => await _context.Set<WebhookDeadLetter>()
            .Where(d => d.Status == WebhookDeadLetterStatus.Pending && d.NextRetryAt <= asOfDate)
            .OrderBy(d => d.NextRetryAt)
            .Take(50) // Batch limit
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<(IReadOnlyList<WebhookDeadLetter> Items, int TotalCount)> GetPagedAsync(
        WebhookDeadLetterStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Set<WebhookDeadLetter>().AsQueryable();
        if (status.HasValue) query = query.Where(d => d.Status == status.Value);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<WebhookDeadLetter?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Set<WebhookDeadLetter>()
            .FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);
}
