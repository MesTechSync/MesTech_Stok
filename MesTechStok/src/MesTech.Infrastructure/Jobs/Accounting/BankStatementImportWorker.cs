using MesTech.Infrastructure.Banking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// Banka ekstre dosyalarini yapilandirilan kaynaklardan import eden Hangfire worker.
/// Yapilandirilan dizindeki dosyalari tarar ve BankStatementImportService ile isler.
/// Her gun 04:00'da calisir.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class BankStatementImportWorker : IAccountingJob
{
    public string JobId => "accounting-bank-statement-import";
    public string CronExpression => "0 4 * * *"; // Her gun 04:00

    private readonly BankStatementImportService _importService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BankStatementImportWorker> _logger;

    public BankStatementImportWorker(
        BankStatementImportService importService,
        IConfiguration configuration,
        ILogger<BankStatementImportWorker> logger)
    {
        _importService = importService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Banka ekstre import basliyor...", JobId);

        var importPaths = _configuration.GetSection("Accounting:BankStatementImport:Paths")
            .Get<string[]>() ?? Array.Empty<string>();

        var defaultBankAccountId = _configuration["Accounting:BankStatementImport:DefaultBankAccountId"];

        if (importPaths.Length == 0)
        {
            _logger.LogInformation(
                "[{JobId}] Import dizini yapilandirilmamis (Accounting:BankStatementImport:Paths), atlaniyor",
                JobId);
            return;
        }

        if (!Guid.TryParse(defaultBankAccountId, out var bankAccountId))
        {
            _logger.LogWarning(
                "[{JobId}] DefaultBankAccountId yapilandirilmamis veya gecersiz, atlaniyor",
                JobId);
            return;
        }

        var totalImported = 0;
        var totalDuplicates = 0;
        var errors = 0;

        foreach (var importPath in importPaths)
        {
            ct.ThrowIfCancellationRequested();

            if (!Directory.Exists(importPath))
            {
                _logger.LogWarning("[{JobId}] Import dizini bulunamadi: {Path}", JobId, importPath);
                continue;
            }

            var files = Directory.GetFiles(importPath, "*.*")
                .Where(f => IsStatementFile(f))
                .ToList();

            _logger.LogInformation(
                "[{JobId}] {FileCount} ekstre dosyasi bulundu: {Path}",
                JobId, files.Count, importPath);

            foreach (var filePath in files)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await using var stream = File.OpenRead(filePath);
                    var result = await _importService.ImportAsync(stream, bankAccountId, ct: ct).ConfigureAwait(false);

                    totalImported += result.NewTransactions;
                    totalDuplicates += result.DuplicateCount;

                    _logger.LogInformation(
                        "[{JobId}] Dosya islendi: {File} — {New} yeni, {Dup} tekrar ({Format})",
                        JobId, Path.GetFileName(filePath),
                        result.NewTransactions, result.DuplicateCount, result.Format);

                    // Islenmis dosyayi arsive tasi
                    MoveToArchive(filePath, importPath);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex,
                        "[{JobId}] Dosya import hatasi: {File}", JobId, filePath);
                }
            }
        }

        _logger.LogInformation(
            "[{JobId}] Import tamamlandi — {Imported} yeni, {Duplicates} tekrar, {Errors} hata",
            JobId, totalImported, totalDuplicates, errors);
    }

    /// <summary>
    /// Dosya uzantisina gore ekstre dosyasi mi kontrol eder.
    /// </summary>
    private static bool IsStatementFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".ofx" or ".qfx" or ".sta" or ".mt940" or ".940" or ".xml" or ".camt053";
    }

    /// <summary>
    /// Islenmis dosyayi "archive" alt dizinine tasir.
    /// </summary>
    private void MoveToArchive(string filePath, string basePath)
    {
        try
        {
            var archiveDir = Path.Combine(basePath, "archive");
            Directory.CreateDirectory(archiveDir);

            var archiveName = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{Path.GetExtension(filePath)}";
            var archivePath = Path.Combine(archiveDir, archiveName);

            File.Move(filePath, archivePath);
            _logger.LogDebug("[{JobId}] Dosya arsive tasinildi: {Archive}", JobId, archivePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{JobId}] Dosya arsive tasinamadi: {File}", JobId, filePath);
        }
    }
}
