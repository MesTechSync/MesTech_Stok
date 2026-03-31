using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// InMemory IAccessLogRepository — Headless LogViewerAvaloniaView için.
/// Gerçek Seq/PostgreSQL query yerine sabit audit log verileri döndürür.
/// DEV 4 — Katman 1.5 Grup C bağımlılık çözümü.
/// </summary>
public sealed class InMemoryAccessLogRepository : IAccessLogRepository
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    private static readonly List<AccessLog> _seedLogs = GenerateSeedLogs();

    private static List<AccessLog> GenerateSeedLogs()
    {
        var now = DateTime.UtcNow;
        return
        [
            new AccessLog
            {
                TenantId = TestTenantId, UserId = TestUserId,
                Action = "Login", Resource = "Auth/Login",
                IsAllowed = true, AccessTime = now.AddMinutes(-120),
                IpAddress = "192.168.1.10", UserAgent = "MesTech Desktop/1.0"
            },
            new AccessLog
            {
                TenantId = TestTenantId, UserId = TestUserId,
                Action = "Error: NullReferenceException in ProductSync", Resource = "Platform/Sync",
                IsAllowed = true, AccessTime = now.AddMinutes(-90),
                IpAddress = "192.168.1.10"
            },
            new AccessLog
            {
                TenantId = TestTenantId, UserId = TestUserId,
                Action = "Warning: Rate limit approaching for Trendyol API", Resource = "Integration/Trendyol",
                IsAllowed = true, AccessTime = now.AddMinutes(-60),
                IpAddress = "192.168.1.10"
            },
            new AccessLog
            {
                TenantId = TestTenantId, UserId = TestUserId,
                Action = "Export stock report", Resource = "Reports/StockExport",
                IsAllowed = true, AccessTime = now.AddMinutes(-45),
                IpAddress = "192.168.1.10"
            },
            new AccessLog
            {
                TenantId = TestTenantId, UserId = TestUserId,
                Action = "Debug: Cache invalidated for product catalog", Resource = "Cache/Products",
                IsAllowed = true, AccessTime = now.AddMinutes(-30),
                IpAddress = "192.168.1.10"
            },
            new AccessLog
            {
                TenantId = TestTenantId, UserId = TestUserId,
                Action = "Create invoice INV-2026-0042", Resource = "Invoice/Create",
                IsAllowed = true, AccessTime = now.AddMinutes(-15),
                IpAddress = "192.168.1.10"
            },
            new AccessLog
            {
                TenantId = TestTenantId, UserId = TestUserId,
                Action = "Error: Connection timeout to RabbitMQ", Resource = "Messaging/RabbitMQ",
                IsAllowed = false, AccessTime = now.AddMinutes(-5),
                IpAddress = "192.168.1.10"
            },
        ];
    }

    public Task<IReadOnlyList<AccessLog>> GetPagedAsync(
        Guid tenantId, DateTime? from, DateTime? to, Guid? userId,
        string? action, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _seedLogs.AsEnumerable();

        if (from.HasValue)
            query = query.Where(l => l.AccessTime >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.AccessTime <= to.Value);
        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action.Contains(action, StringComparison.OrdinalIgnoreCase));

        var result = query
            .OrderByDescending(l => l.AccessTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<AccessLog>>(result);
    }

    public Task AddAsync(AccessLog log, CancellationToken ct = default)
    {
        _seedLogs.Add(log);
        return Task.CompletedTask;
    }
}
