using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Interfaces;

namespace MesTech.Infrastructure.Persistence.Repositories.Calendar;

public class CalendarEventRepository : ICalendarEventRepository
{
    private readonly AppDbContext _context;

    public CalendarEventRepository(AppDbContext context) => _context = context;

    public async Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CalendarEvent>> GetByDateRangeAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.CalendarEvents
            .Where(e => e.TenantId == tenantId
                     && e.StartAt >= from
                     && e.StartAt <= to)
            .OrderBy(e => e.StartAt)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(CalendarEvent calendarEvent, CancellationToken ct = default)
        => await _context.CalendarEvents.AddAsync(calendarEvent, ct).ConfigureAwait(false);
}
