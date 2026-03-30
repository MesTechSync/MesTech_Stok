using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm platformlar için periyodik sipariş çekme (order pull) job'ı.
/// Platform kodu parametre olarak alınır — her platform ayrı Hangfire recurring job.
///
/// Pattern: PullOrdersAsync(son 2 saat) → MediatR command dispatch (handler tarafında)
///
/// G497 FIX: Sadece TrendyolOrderSyncJob vardı, diğer 14 platformun
/// order pull safety net'i yoktu. Bu job tüm IOrderCapableAdapter'ları kapsar.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class GenericPlatformOrderSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly ILogger<GenericPlatformOrderSyncJob> _logger;

    public GenericPlatformOrderSyncJob(
        IAdapterFactory factory,
        ILogger<GenericPlatformOrderSyncJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[OrderSync] {Platform} sipariş sync başlıyor...", platformCode);

        var adapter = _factory.ResolveCapability<IOrderCapableAdapter>(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning(
                "[OrderSync] {Platform} IOrderCapableAdapter bulunamadı — atlaniyor", platformCode);
            return;
        }

        try
        {
            var since = DateTime.UtcNow.AddHours(-2);
            var orders = await adapter.PullOrdersAsync(since, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[OrderSync] {Platform} TAMAMLANDI — {Count} sipariş çekildi (son 2 saat)",
                platformCode, orders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[OrderSync] {Platform} sipariş sync BAŞARISIZ", platformCode);
            throw; // Hangfire retry
        }
    }
}
