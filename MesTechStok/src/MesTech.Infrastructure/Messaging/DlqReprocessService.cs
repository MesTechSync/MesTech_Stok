using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// DLQ'daki mesajlari orijinal queue'ya geri gonderen servis.
/// Manuel tetiklenir (admin endpoint).
/// Max 3 reprocess denemesi — sonra permanent-dlq'ya tasi.
/// </summary>
public sealed class DlqReprocessService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DlqReprocessService> _logger;

    public DlqReprocessService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DlqReprocessService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DlqReprocessResult> ReprocessAsync(
        string queueName, int maxMessages = 10, CancellationToken ct = default)
    {
        var result = new DlqReprocessResult { QueueName = queueName };

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

        var errorQueue = queueName.EndsWith("_error") ? queueName : $"{queueName}_error";
        var originalQueue = errorQueue.Replace("_error", "");

        try
        {
            // Get messages from DLQ via Management API
            var getBody = JsonSerializer.Serialize(new
            {
                count = maxMessages,
                ackmode = "ack_requeue_false",
                encoding = "auto"
            });

            var getContent = new StringContent(getBody, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(
                $"{baseUrl}/api/queues/%2f/{Uri.EscapeDataString(errorQueue)}/get", getContent, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[DLQ Reprocess] Failed to get messages from {Queue}: {Status}",
                    errorQueue, response.StatusCode);
                result.ErrorMessage = $"Failed to read from {errorQueue}: {response.StatusCode}";
                return result;
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseContent);
            var messages = doc.RootElement;

            foreach (var msg in messages.EnumerateArray())
            {
                result.TotalFound++;

                // Check reprocess count from headers
                var reprocessCount = 0;
                if (msg.TryGetProperty("properties", out var props) &&
                    props.TryGetProperty("headers", out var headers) &&
                    headers.TryGetProperty("x-reprocess-count", out var countProp) &&
                    countProp.TryGetInt32(out var parsedCount))
                {
                    reprocessCount = parsedCount;
                }

                if (reprocessCount >= 3)
                {
                    _logger.LogWarning(
                        "[DLQ Reprocess] Message exceeded max retries (3) — moving to permanent DLQ. Queue={Queue}",
                        errorQueue);
                    result.PermanentDlq++;
                    continue;
                }

                // Republish to original queue with incremented reprocess count
                var publishBody = JsonSerializer.Serialize(new
                {
                    properties = new
                    {
                        headers = new Dictionary<string, object>
                        {
                            ["x-reprocess-count"] = reprocessCount + 1,
                            ["x-original-queue"] = errorQueue,
                            ["x-reprocessed-at"] = DateTimeOffset.UtcNow.ToString("O")
                        }
                    },
                    routing_key = originalQueue,
                    payload = msg.TryGetProperty("payload", out var payload) ? payload.GetString() : "",
                    payload_encoding = "string"
                });

                var publishContent = new StringContent(publishBody, System.Text.Encoding.UTF8, "application/json");
                var publishResponse = await client.PostAsync(
                    $"{baseUrl}/api/exchanges/%2f/amq.default/publish", publishContent, ct).ConfigureAwait(false);

                if (publishResponse.IsSuccessStatusCode)
                {
                    result.Reprocessed++;
                    _logger.LogInformation(
                        "[DLQ Reprocess] Message republished to {Queue} (attempt {Attempt})",
                        originalQueue, reprocessCount + 1);
                }
                else
                {
                    result.Failed++;
                    _logger.LogWarning(
                        "[DLQ Reprocess] Failed to republish to {Queue}: {Status}",
                        originalQueue, publishResponse.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DLQ Reprocess] Error processing {Queue}", errorQueue);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<List<DlqQueueStatus>> GetDlqStatusAsync(CancellationToken ct = default)
    {
        var result = new List<DlqQueueStatus>();

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
            var response = await client.GetAsync($"{baseUrl}/api/queues/%2f", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return result;

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            foreach (var queue in doc.RootElement.EnumerateArray())
            {
                var name = queue.GetProperty("name").GetString() ?? "";
                if (!name.EndsWith("_error")) continue;

                var messageCount = queue.TryGetProperty("messages", out var msgProp)
                    ? msgProp.GetInt32() : 0;

                result.Add(new DlqQueueStatus
                {
                    QueueName = name,
                    MessageCount = messageCount
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DLQ Status] Failed to fetch DLQ status");
        }

        return result;
    }
}

public sealed class DlqReprocessResult
{
    public string QueueName { get; set; } = string.Empty;
    public int TotalFound { get; set; }
    public int Reprocessed { get; set; }
    public int Failed { get; set; }
    public int PermanentDlq { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class DlqQueueStatus
{
    public string QueueName { get; set; } = string.Empty;
    public int MessageCount { get; set; }
}
