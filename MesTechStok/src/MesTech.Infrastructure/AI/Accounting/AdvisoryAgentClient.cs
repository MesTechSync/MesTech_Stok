using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.AI.Accounting;

/// <summary>
/// MESA OS Gunluk Mali Brifing arayuzu.
/// </summary>
public interface IAdvisoryAgentClient
{
    Task<DailyBriefing> GenerateBriefingAsync(Guid tenantId, DateTime date, CancellationToken ct = default);
}

/// <summary>
/// Gunluk mali brifing sonucu.
/// </summary>
public record DailyBriefing(
    string Summary,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> Alerts,
    decimal TotalRevenue,
    decimal NetProfit,
    int OrderCount);

/// <summary>
/// MESA OS Advisory Agent gercek HTTP istemcisi.
/// POST localhost:3101/api/v1/accounting/advisory/daily
/// Demir Kural #12: MESA kopunca basit ozet uretir (AI'siz fallback).
/// </summary>
public sealed class AdvisoryAgentClient : IAdvisoryAgentClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdvisoryAgentClient> _logger;

    public AdvisoryAgentClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AdvisoryAgentClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["Mesa:Accounting:BaseUrl"] ?? "http://localhost:3101";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.GetValue<int>("Mesa:Advisory:TimeoutSeconds", 30));
    }

    public async Task<DailyBriefing> GenerateBriefingAsync(
        Guid tenantId, DateTime date, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                tenantId,
                date = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/accounting/advisory/daily", payload, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[MESA Advisory] Briefing failed: {StatusCode}", response.StatusCode);
                return GenerateFallbackBriefing(date);
            }

            var result = await response.Content.ReadFromJsonAsync<MesaAdvisoryResponse>(
                cancellationToken: ct).ConfigureAwait(false);

            if (result is null)
            {
                _logger.LogWarning("[MESA Advisory] Response deserialization failed");
                return GenerateFallbackBriefing(date);
            }

            _logger.LogInformation(
                "[MESA Advisory] Briefing basarili: tarih={Date}, gelir={Revenue:N2}",
                date.ToString("yyyy-MM-dd"), result.TotalRevenue);

            return new DailyBriefing(
                result.Summary ?? $"{date:yyyy-MM-dd} tarihli brifing hazir.",
                result.Recommendations ?? Array.Empty<string>(),
                result.Alerts ?? Array.Empty<string>(),
                result.TotalRevenue,
                result.NetProfit,
                result.OrderCount);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "[MESA Advisory] MESA OS unreachable, using fallback briefing");
            return GenerateFallbackBriefing(date);
        }
    }

    private static DailyBriefing GenerateFallbackBriefing(DateTime date)
    {
        return new DailyBriefing(
            Summary: $"{date:yyyy-MM-dd} tarihli mali ozet — MESA AI baglantisi kurulamadi, veriler eksik olabilir.",
            Recommendations: new[] { "MESA AI baglantisinizi kontrol edin." },
            Alerts: new[] { "MESA OS erisim sorunu — brifing verileri eksik." },
            TotalRevenue: 0m,
            NetProfit: 0m,
            OrderCount: 0);
    }
}

/// <summary>
/// MESA OS /api/v1/accounting/advisory/daily yanit modeli.
/// </summary>
internal record MesaAdvisoryResponse
{
    public string? Summary { get; init; }
    public string[]? Recommendations { get; init; }
    public string[]? Alerts { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal NetProfit { get; init; }
    public int OrderCount { get; init; }
}
