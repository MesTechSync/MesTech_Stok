using System.Text.Json;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// Hangfire job olarak her 5 dakikada bir DLQ queue'larinin derinligini
/// kontrol eder. Derinlik > 0 ise alarm gonderir.
/// RabbitMQ Management API kullanir: GET /api/queues/{vhost}/{queue_name}
/// </summary>
public sealed class DlqMonitorService
{
    /// <summary>Auto-reprocess threshold — bu sayinin altindaki mesajlar otomatik retry edilir.</summary>
    private const int AutoReprocessThreshold = 10;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMesaBotService _bot;
    private readonly DlqReprocessService _reprocessService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DlqMonitorService> _logger;

    public DlqMonitorService(
        IHttpClientFactory httpClientFactory,
        IMesaBotService bot,
        DlqReprocessService reprocessService,
        IConfiguration configuration,
        ILogger<DlqMonitorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _bot = bot;
        _reprocessService = reprocessService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CheckDlqDepthAsync(CancellationToken ct = default)
    {
        var rabbitHost = _configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = _configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = _configuration["RabbitMQ:Password"] ?? "guest";
        var managementPort = _configuration["RabbitMQ:ManagementPort"] ?? "15672";

        var client = _httpClientFactory.CreateClient("RabbitMqManagement");
        var scheme = _configuration["RabbitMQ:ManagementScheme"] ?? "http";
        var baseUrl = $"{scheme}://{rabbitHost}:{managementPort}";

        var authBytes = System.Text.Encoding.ASCII.GetBytes($"{rabbitUser}:{rabbitPass}");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(authBytes));

        try
        {
            using var response = await client.GetAsync($"{baseUrl}/api/queues/%2f", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DLQ Monitor] RabbitMQ Management API unreachable: {StatusCode}",
                    response.StatusCode);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);
            var queues = doc.RootElement;

            foreach (var queue in queues.EnumerateArray())
            {
                var name = queue.GetProperty("name").GetString() ?? "";
                if (!name.EndsWith("_error"))
                    continue;

                var messageCount = queue.TryGetProperty("messages", out var msgProp)
                    ? msgProp.GetInt32() : 0;

                MesaMetrics.DlqDepth.Record(messageCount,
                    new KeyValuePair<string, object?>("queue", name));

                if (messageCount > 0)
                {
                    _logger.LogError(
                        "[DLQ Monitor] DLQ mesaji tespit edildi: Queue={Queue}, Depth={Depth}",
                        name, messageCount);

                    // DEV3 TUR7: Auto-reprocess — threshold altinda otomatik retry
                    if (messageCount <= AutoReprocessThreshold)
                    {
                        try
                        {
                            var result = await _reprocessService.ReprocessAsync(
                                name, maxMessages: messageCount, ct).ConfigureAwait(false);
                            _logger.LogInformation(
                                "[DLQ Monitor] Auto-reprocess: Queue={Queue}, Reprocessed={Reprocessed}, Failed={Failed}",
                                name, result.Reprocessed, result.Failed);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[DLQ Monitor] Auto-reprocess failed: Queue={Queue}", name);
                        }
                    }
                    else
                    {
                        _logger.LogError(
                            "[DLQ Monitor] DLQ depth {Depth} exceeds auto-reprocess threshold ({Threshold}). Manuel mudahale gerekli: Queue={Queue}",
                            messageCount, AutoReprocessThreshold, name);
                    }

                    try
                    {
                        await _bot.SendTelegramAlertAsync(
                            "mestech-alerts",
                            $"⚠️ DLQ ALARM: {name} queue'sunda {messageCount} basarisiz mesaj var!" +
                            (messageCount <= AutoReprocessThreshold ? " (auto-reprocess tetiklendi)" : " (MANUEL MÜDAHALE GEREKLİ)"),
                            TelegramAlertLevel.Warning,
                            ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[DLQ Monitor] Telegram alarm gonderilemedi");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DLQ Monitor] DLQ depth check failed");
        }
    }
}
