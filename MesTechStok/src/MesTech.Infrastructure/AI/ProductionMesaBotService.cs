using System.Net.Http.Json;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace MesTech.Infrastructure.AI;

/// <summary>
/// Gercek MESA OS Bot servisi — HTTP REST API uzerinden WhatsApp/Telegram/Bulk bildirim gonderir.
/// Feature flag: Mesa:UseProductionBridge=true olunca MockMesaBotService yerine bu kullanilir.
/// Demir Kural: MESA kopunca veya hata verince graceful fallback — MesTech calismaya devam eder.
/// Endpoint: Mesa:ApiUrl (appsettings: http://localhost:3000/api)
/// </summary>
public sealed class ProductionMesaBotService : IMesaBotService
{
    private readonly HttpClient _httpClient;
    private readonly MockMesaBotService _mockFallback;
    private readonly ILogger<ProductionMesaBotService> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public ProductionMesaBotService(
        HttpClient httpClient,
        IConfiguration configuration,
        MockMesaBotService mockFallback,
        ILogger<ProductionMesaBotService> logger)
    {
        _httpClient = httpClient;
        _mockFallback = mockFallback;
        _logger = logger;

        var baseUrl = configuration["Mesa:ApiUrl"] ?? "http://localhost:3000/api";
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("Mesa:BotTimeoutSeconds", 15));

        var apiKey = configuration["Mesa:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<OperationCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(45),
                onBreak: (ex, ts) => { MesaMetrics.RecordCircuitState("mesa_bot", 2); _logger.LogWarning(
                    "[MESA Bot] Circuit OPEN — {Duration}s. Error: {Error}",
                    ts.TotalSeconds, ex.Message); },
                onReset: () => { MesaMetrics.RecordCircuitState("mesa_bot", 0); _logger.LogInformation(
                    "[MESA Bot] Circuit CLOSED — MESA Bot baglantisi yeniden aktif"); },
                onHalfOpen: () => { MesaMetrics.RecordCircuitState("mesa_bot", 1); _logger.LogInformation(
                    "[MESA Bot] Circuit HALF-OPEN — test cagrisi yapiliyor"); });
    }

    public async Task<bool> SendWhatsAppNotificationAsync(
        string phoneNumber, string templateName,
        Dictionary<string, string> templateData,
        CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                MesaMetrics.BotSendTotal.Add(1, new KeyValuePair<string, object?>("channel", "whatsapp"));
                var payload = new
                {
                    phone = phoneNumber,
                    template = templateName,
                    data = templateData
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    "v1/bot/whatsapp/send", payload, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA Bot] WhatsApp gonderim basarisiz: {StatusCode} — fallback mock (phone={Phone}, template={Template})",
                        response.StatusCode, MaskPhone(phoneNumber), templateName);
                    return await _mockFallback.SendWhatsAppNotificationAsync(
                        phoneNumber, templateName, templateData, ct).ConfigureAwait(false);
                }

                _logger.LogInformation(
                    "[MESA Bot] WhatsApp basarili: phone={Phone}, template={Template}",
                    MaskPhone(phoneNumber), templateName);
                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA Bot] MESA OS unreachable, falling back to mock (WhatsApp, phone={Phone})",
                MaskPhone(phoneNumber));
            return await _mockFallback.SendWhatsAppNotificationAsync(
                phoneNumber, templateName, templateData, ct).ConfigureAwait(false);
        }
    }

    public async Task<bool> SendTelegramAlertAsync(
        string channelId, string message,
        TelegramAlertLevel level = TelegramAlertLevel.Info,
        CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                MesaMetrics.BotSendTotal.Add(1, new KeyValuePair<string, object?>("channel", "telegram"));
                var payload = new
                {
                    chatId = channelId,
                    message,
                    level = level.ToString().ToLowerInvariant()
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    "v1/bot/telegram/alert", payload, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA Bot] Telegram gonderim basarisiz: {StatusCode} — fallback mock (channel={Channel})",
                        response.StatusCode, channelId);
                    return await _mockFallback.SendTelegramAlertAsync(
                        channelId, message, level, ct).ConfigureAwait(false);
                }

                _logger.LogInformation(
                    "[MESA Bot] Telegram basarili: channel={Channel}, level={Level}",
                    channelId, level);
                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA Bot] MESA OS unreachable, falling back to mock (Telegram, channel={Channel})",
                channelId);
            return await _mockFallback.SendTelegramAlertAsync(
                channelId, message, level, ct).ConfigureAwait(false);
        }
    }

    public async Task<bool> SendBulkNotificationAsync(
        NotificationChannel channel, List<string> recipients,
        string templateName, Dictionary<string, string> data,
        CancellationToken ct = default)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                MesaMetrics.BotSendTotal.Add(1, new KeyValuePair<string, object?>("channel", channel.ToString().ToLowerInvariant()));
                var payload = new
                {
                    channel = channel.ToString().ToLowerInvariant(),
                    recipients,
                    template = templateName,
                    data
                };

                using var response = await _httpClient.PostAsJsonAsync(
                    "v1/bot/bulk/send", payload, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[MESA Bot] Bulk gonderim basarisiz: {StatusCode} — fallback mock (channel={Channel}, count={Count})",
                        response.StatusCode, channel, recipients.Count);
                    return await _mockFallback.SendBulkNotificationAsync(
                        channel, recipients, templateName, data, ct).ConfigureAwait(false);
                }

                _logger.LogInformation(
                    "[MESA Bot] Bulk basarili: channel={Channel}, count={Count}, template={Template}",
                    channel, recipients.Count, templateName);
                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or OperationCanceledException or BrokenCircuitException)
        {
            _logger.LogError(ex,
                "[MESA Bot] MESA OS unreachable, falling back to mock (Bulk, channel={Channel})",
                channel);
            return await _mockFallback.SendBulkNotificationAsync(
                channel, recipients, templateName, data, ct).ConfigureAwait(false);
        }
    }

    private static string MaskPhone(string phone) =>
        phone.Length > 5
            ? phone[..3] + new string('*', phone.Length - 5) + phone[^2..]
            : "***";
}
