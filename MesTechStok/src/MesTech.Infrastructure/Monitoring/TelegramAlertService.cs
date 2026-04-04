using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Monitoring;

public record PrometheusAlert(
    string AlertName,
    string Severity,
    string Summary,
    Dictionary<string, string> Labels);

public sealed class TelegramAlertService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramAlertService> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _throttleCache = new();
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(15);

    public TelegramAlertService(
        HttpClient httpClient,
        ILogger<TelegramAlertService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendAlertAsync(PrometheusAlert alert, CancellationToken ct = default)
    {
        var alertKey = $"{alert.AlertName}:{string.Join(",", alert.Labels.Select(l => $"{l.Key}={l.Value}"))}";

        if (_throttleCache.TryGetValue(alertKey, out var lastSent)
            && DateTimeOffset.UtcNow - lastSent < ThrottleWindow)
        {
            _logger.LogDebug("Alert {AlertKey} throttled — last sent {LastSent}", alertKey, lastSent);
            return false;
        }

        var chatId = Environment.GetEnvironmentVariable("MESTECH_ALERT_CHAT_ID");
        var botToken = Environment.GetEnvironmentVariable("MESTECH_TELEGRAM_BOT_TOKEN");

        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogWarning("Telegram alert skipped — MESTECH_ALERT_CHAT_ID or MESTECH_TELEGRAM_BOT_TOKEN not set");
            return false;
        }

        var emoji = alert.Severity.ToLowerInvariant() switch
        {
            "critical" => "\ud83d\udd34",
            "warning" => "\ud83d\udfe1",
            _ => "\u2139\ufe0f"
        };

        var labelText = string.Join("\n", alert.Labels.Select(l => $"  {l.Key}: {l.Value}"));

        var message = $"""
            {emoji} *{EscapeMarkdown(alert.AlertName)}* [{alert.Severity.ToUpperInvariant()}]

            {EscapeMarkdown(alert.Summary)}

            *Labels:*
            {EscapeMarkdown(labelText)}

            _MesTech Monitoring_
            """;

        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        try
        {
            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "MarkdownV2",
                disable_notification = alert.Severity.ToLowerInvariant() != "critical"
            };

            using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);

            if (response.IsSuccessStatusCode)
            {
                _throttleCache[alertKey] = DateTimeOffset.UtcNow;
                _logger.LogInformation("Telegram alert sent: {AlertName} ({Severity})", alert.AlertName, alert.Severity);
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Telegram API error {StatusCode}: {Body}", response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram alert for {AlertName}", alert.AlertName);
            return false;
        }
    }

    /// <summary>
    /// Removes expired entries from the throttle cache.
    /// Call periodically from a background service if needed.
    /// </summary>
    public void PurgeExpiredThrottleEntries()
    {
        var cutoff = DateTimeOffset.UtcNow - ThrottleWindow;
        var expired = _throttleCache
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expired)
        {
            _throttleCache.TryRemove(key, out _);
        }

        if (expired.Count > 0)
            _logger.LogDebug("Purged {Count} expired throttle entries", expired.Count);
    }

    private static string EscapeMarkdown(string text)
    {
        // MarkdownV2 requires escaping these characters
        var specialChars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var c in specialChars)
        {
            text = text.Replace(c.ToString(), $"\\{c}");
        }
        return text;
    }
}
