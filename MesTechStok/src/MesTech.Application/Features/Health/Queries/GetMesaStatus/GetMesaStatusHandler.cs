using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Health.Queries.GetMesaStatus;

public sealed class GetMesaStatusHandler : IRequestHandler<GetMesaStatusQuery, MesaStatusDto>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GetMesaStatusHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GetMesaStatusHandler(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GetMesaStatusHandler> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MesaStatusDto> Handle(GetMesaStatusQuery request, CancellationToken cancellationToken)
    {
        var bridgeUrl = _configuration["Mesa:BridgeUrl"] ?? "http://localhost:3105";
        var healthEndpoint = $"{bridgeUrl.TrimEnd('/')}/health";

        _logger.LogDebug("Checking MESA OS Bridge status at {Url}", healthEndpoint);

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = _httpClientFactory.CreateClient("MesaBridge");
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync(healthEndpoint, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new MesaStatusDto
                {
                    IsConnected = false,
                    BridgeUrl = bridgeUrl,
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var health = JsonSerializer.Deserialize<MesaHealthResponse>(json, JsonOptions);

            return new MesaStatusDto
            {
                IsConnected = true,
                LastHeartbeat = health?.LastHeartbeat ?? DateTime.UtcNow,
                Version = health?.Version,
                ActiveConsumers = health?.ActiveConsumers ?? 0,
                BridgeUrl = bridgeUrl,
                FeatureFlags = health?.FeatureFlags ?? new Dictionary<string, bool>(),
                ResponseTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning(ex, "MESA OS Bridge unreachable at {Url}", healthEndpoint);

            return new MesaStatusDto
            {
                IsConnected = false,
                BridgeUrl = bridgeUrl,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }

    private sealed class MesaHealthResponse
    {
        public DateTime? LastHeartbeat { get; set; }
        public string? Version { get; set; }
        public int ActiveConsumers { get; set; }
        public Dictionary<string, bool> FeatureFlags { get; set; } = new();
    }
}
