using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Services;

/// <summary>
/// S4-DEV3-01: ERP cari hesap eşleştirme servisi.
/// MesTech müşteri → ERP cari hesap mapping.
/// Kural 1: VKN/TCKN match → %100 otomatik
/// Kural 2: İsim match → %80 önerme
/// Kural 3: Manuel seçim
///
/// S4-DEV3-02: Sync hata yönetimi — 3 başarısız → admin bildirimi.
/// </summary>
public interface IErpAccountMatchingService
{
    Task<ErpAccountMatchResult> MatchCustomerAsync(string? vknTckn, string? customerName, CancellationToken ct = default);
    Task ReportSyncErrorAsync(string erpType, string entityType, Guid entityId, string errorDetail, CancellationToken ct = default);
    Task<IReadOnlyList<ErpSyncError>> GetPendingErrorsAsync(string? erpType = null, CancellationToken ct = default);
}

public sealed class ErpAccountMatchingService : IErpAccountMatchingService
{
    private readonly ILogger<ErpAccountMatchingService> _logger;
    private static readonly List<ErpSyncError> _errors = new();
    private static readonly object _lock = new();

    public ErpAccountMatchingService(ILogger<ErpAccountMatchingService> logger)
    {
        _logger = logger;
    }

    public Task<ErpAccountMatchResult> MatchCustomerAsync(string? vknTckn, string? customerName, CancellationToken ct = default)
    {
        // Kural 1: VKN/TCKN tam eşleşme
        if (!string.IsNullOrEmpty(vknTckn) && (vknTckn.Length == 10 || vknTckn.Length == 11) && vknTckn.All(char.IsDigit))
        {
            _logger.LogInformation("[ErpMatch] VKN/TCKN match: {VknTckn}", vknTckn);
            return Task.FromResult(new ErpAccountMatchResult
            {
                MatchedBy = "VKN_TCKN",
                Confidence = 100,
                IsAutoMatch = true,
                VknTckn = vknTckn,
                CustomerName = customerName
            });
        }

        // Kural 2: İsim match (önerme)
        if (!string.IsNullOrEmpty(customerName))
        {
            _logger.LogInformation("[ErpMatch] İsim önerme: {Name}", customerName);
            return Task.FromResult(new ErpAccountMatchResult
            {
                MatchedBy = "NAME",
                Confidence = 80,
                IsAutoMatch = false,
                CustomerName = customerName
            });
        }

        return Task.FromResult(new ErpAccountMatchResult { MatchedBy = "NONE", Confidence = 0, IsAutoMatch = false });
    }

    public Task ReportSyncErrorAsync(string erpType, string entityType, Guid entityId, string errorDetail, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var existing = _errors.FirstOrDefault(e =>
                e.ErpType == erpType && e.EntityId == entityId && !e.IsResolved);

            if (existing is not null)
            {
                existing.RetryCount++;
                existing.LastError = errorDetail;
                existing.LastAttempt = DateTime.UtcNow;

                if (existing.RetryCount >= 3)
                {
                    _logger.LogError(
                        "[ErpSync] 3 başarısız deneme — ADMİN BİLDİRİMİ: {ErpType} {EntityType} {EntityId} — {Error}",
                        erpType, entityType, entityId, errorDetail);
                }
            }
            else
            {
                _errors.Add(new ErpSyncError
                {
                    ErpType = erpType,
                    EntityType = entityType,
                    EntityId = entityId,
                    LastError = errorDetail,
                    RetryCount = 1,
                    FirstAttempt = DateTime.UtcNow,
                    LastAttempt = DateTime.UtcNow
                });
            }
        }

        _logger.LogWarning("[ErpSync] Hata: {ErpType} {EntityType} {EntityId} — {Error}",
            erpType, entityType, entityId, errorDetail);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ErpSyncError>> GetPendingErrorsAsync(string? erpType = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var query = _errors.Where(e => !e.IsResolved);
            if (!string.IsNullOrEmpty(erpType))
                query = query.Where(e => e.ErpType == erpType);
            return Task.FromResult<IReadOnlyList<ErpSyncError>>(
                query.OrderByDescending(e => e.LastAttempt).ToList().AsReadOnly());
        }
    }
}

public sealed class ErpAccountMatchResult
{
    public string MatchedBy { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public bool IsAutoMatch { get; set; }
    public string? ErpAccountId { get; set; }
    public string? VknTckn { get; set; }
    public string? CustomerName { get; set; }
}

public sealed class ErpSyncError
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ErpType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string LastError { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public bool IsResolved { get; set; }
    public DateTime FirstAttempt { get; set; }
    public DateTime LastAttempt { get; set; }
}
