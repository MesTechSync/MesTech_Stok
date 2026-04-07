using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Services;

/// <summary>
/// D12-13: Platform sync sorunlarını takip eder.
/// Barkod çakışması, kategori mismatch, image rejection vb. raporlanır.
/// </summary>
public interface IPlatformSyncIssueService
{
    Task ReportIssueAsync(Guid productId, PlatformType platform, SyncIssueType type,
        string description, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformSyncIssue>> GetOpenIssuesAsync(PlatformType? platform = null,
        CancellationToken ct = default);
    Task<bool> ResolveAsync(Guid issueId, CancellationToken ct = default);
}

public enum SyncIssueType
{
    BarcodeConflict,
    CategoryMismatch,
    ImageRejected,
    PriceViolation,
    StockMismatch,
    AttributeMissing,
    BrandNotFound,
    ModelCodeConflict,
    ApiError,
    RateLimited
}

public sealed class PlatformSyncIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public PlatformType Platform { get; set; }
    public SyncIssueType IssueType { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public sealed class PlatformSyncIssueService : IPlatformSyncIssueService
{
    private readonly ILogger<PlatformSyncIssueService> _logger;

    // In-memory issue store — production'da DB'ye taşınacak (DEV1 entity gerekli)
    private static readonly List<PlatformSyncIssue> _issues = new();
    private static readonly object _lock = new();

    public PlatformSyncIssueService(ILogger<PlatformSyncIssueService> logger)
    {
        _logger = logger;
    }

    public Task ReportIssueAsync(Guid productId, PlatformType platform, SyncIssueType type,
        string description, CancellationToken ct = default)
    {
        var issue = new PlatformSyncIssue
        {
            ProductId = productId,
            Platform = platform,
            IssueType = type,
            Description = description
        };

        lock (_lock)
        {
            _issues.Add(issue);
        }

        _logger.LogWarning(
            "[SyncIssue] {Platform} {Type}: ProductId={ProductId} — {Description}",
            platform, type, productId, description);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PlatformSyncIssue>> GetOpenIssuesAsync(
        PlatformType? platform = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var query = _issues.Where(i => !i.IsResolved);
            if (platform.HasValue)
                query = query.Where(i => i.Platform == platform.Value);
            return Task.FromResult<IReadOnlyList<PlatformSyncIssue>>(
                query.OrderByDescending(i => i.ReportedAt).ToList().AsReadOnly());
        }
    }

    public Task<bool> ResolveAsync(Guid issueId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var issue = _issues.FirstOrDefault(i => i.Id == issueId && !i.IsResolved);
            if (issue is null) return Task.FromResult(false);
            issue.IsResolved = true;
            issue.ResolvedAt = DateTime.UtcNow;
            _logger.LogInformation("[SyncIssue] RESOLVED: IssueId={IssueId}", issueId);
            return Task.FromResult(true);
        }
    }
}
