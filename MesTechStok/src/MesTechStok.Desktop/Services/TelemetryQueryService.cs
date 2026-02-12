using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Telemetry verilerini sorgulama servisi
    /// UI katmanı için optimized queries
    /// </summary>
    public interface ITelemetryQueryService
    {
        Task<IReadOnlyList<ApiCallLog>> GetRecentAsync(int take = 200, string? endpointContains = null, bool? success = null, string? category = null);
        Task<IReadOnlyList<CircuitStateLog>> GetCircuitStateHistoryAsync(int take = 100);
    }

    public sealed class TelemetryQueryService : ITelemetryQueryService
    {
        private readonly AppDbContext _db;

        public TelemetryQueryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ApiCallLog>> GetRecentAsync(int take = 200, string? endpointContains = null, bool? success = null, string? category = null)
        {
            if (take <= 0) take = 50;
            if (take > 1000) take = 1000;

            var q = _db.ApiCallLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(endpointContains))
                q = q.Where(x => x.Endpoint.Contains(endpointContains));

            if (success.HasValue)
                q = q.Where(x => x.Success == success.Value);

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(x => x.Category == category);

            return await q.OrderByDescending(x => x.TimestampUtc)
                          .Take(take)
                          .ToListAsync();
        }

        public async Task<IReadOnlyList<CircuitStateLog>> GetCircuitStateHistoryAsync(int take = 100)
        {
            if (take <= 0) take = 50;
            if (take > 500) take = 500;

            return await _db.CircuitStateLogs
                           .AsNoTracking()
                           .OrderByDescending(x => x.TransitionTimeUtc)
                           .Take(take)
                           .ToListAsync();
        }
    }
}
