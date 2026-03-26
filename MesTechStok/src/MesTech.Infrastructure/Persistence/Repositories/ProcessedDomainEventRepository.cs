using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProcessedDomainEventRepository : IProcessedDomainEventRepository
{
    private readonly AppDbContext _context;

    public ProcessedDomainEventRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<bool> IsProcessedAsync(Guid eventId, string handlerName, CancellationToken ct = default)
    {
        return await _context.Set<ProcessedDomainEvent>()
            .AnyAsync(e => e.EventId == eventId && e.HandlerName == handlerName, ct)
            .ConfigureAwait(false);
    }

    public async Task MarkAsProcessedAsync(
        Guid eventId, string eventType, Guid tenantId, string handlerName, CancellationToken ct = default)
    {
        var record = new ProcessedDomainEvent
        {
            EventId = eventId,
            EventType = eventType,
            TenantId = tenantId,
            ProcessedAt = DateTime.UtcNow,
            HandlerName = handlerName
        };

        await _context.Set<ProcessedDomainEvent>().AddAsync(record, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
