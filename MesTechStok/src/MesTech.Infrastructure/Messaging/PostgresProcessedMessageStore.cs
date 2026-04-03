using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// Production-grade idempotency store — PostgreSQL tabanlı.
/// InMemory'den farkı: app restart'ta veri KORUNUR.
/// Tablo: processed_messages (message_id, consumer_name, processed_at)
///
/// Migration: EnsureTableExistsAsync() ilk çağrıda otomatik oluşturur.
/// Cleanup: CleanupOldEntriesAsync() 30 gün öncesini siler (Hangfire job ile çağrılabilir).
/// </summary>
public sealed class PostgresProcessedMessageStore : IProcessedMessageStore
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresProcessedMessageStore> _logger;
    private bool _tableEnsured;
    private DateTimeOffset _lastTableCheckAttempt = DateTimeOffset.MinValue;
    private static readonly TimeSpan TableCheckCooldown = TimeSpan.FromMinutes(5);

    public PostgresProcessedMessageStore(
        IConfiguration config,
        ILogger<PostgresProcessedMessageStore> logger)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string eksik — PostgresProcessedMessageStore başlatılamıyor");
        _logger = logger;
    }

    public async Task<bool> IsProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        await EnsureTableExistsAsync(ct).ConfigureAwait(false);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM processed_messages WHERE message_id = @id LIMIT 1", conn);
        cmd.Parameters.AddWithValue("id", messageId.ToString());

        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result is not null;
    }

    public async Task MarkProcessedAsync(Guid messageId, string consumerName, CancellationToken ct = default)
    {
        await EnsureTableExistsAsync(ct).ConfigureAwait(false);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO processed_messages (message_id, consumer_name, processed_at)
              VALUES (@id, @consumer, @now)
              ON CONFLICT (message_id) DO NOTHING", conn);

        cmd.Parameters.AddWithValue("id", messageId.ToString());
        cmd.Parameters.AddWithValue("consumer", consumerName);
        cmd.Parameters.AddWithValue("now", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Eski kayıtları temizle (30 gün öncesi). Hangfire job ile çağırın.</summary>
    public async Task CleanupOldEntriesAsync(int retentionDays = 30, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            "DELETE FROM processed_messages WHERE processed_at < @cutoff", conn);
        cmd.Parameters.AddWithValue("cutoff", DateTime.UtcNow.AddDays(-retentionDays));

        var deleted = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        if (deleted > 0)
            _logger.LogInformation("Processed messages cleanup: {Count} eski kayıt silindi", deleted);
    }

    private async Task EnsureTableExistsAsync(CancellationToken ct)
    {
        if (_tableEnsured) return;

        // Cooldown: don't retry table creation more than once per 5 minutes
        if (DateTimeOffset.UtcNow - _lastTableCheckAttempt < TableCheckCooldown)
            return;

        _lastTableCheckAttempt = DateTimeOffset.UtcNow;

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS processed_messages (
                    message_id VARCHAR(128) PRIMARY KEY,
                    consumer_name VARCHAR(256) NOT NULL,
                    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );
                CREATE INDEX IF NOT EXISTS ix_processed_messages_processed_at
                    ON processed_messages (processed_at);", conn);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            _tableEnsured = true;
            _logger.LogDebug("processed_messages tablosu hazır");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "processed_messages tablosu oluşturulamadı — 5dk sonra tekrar denenecek");
        }
    }
}
