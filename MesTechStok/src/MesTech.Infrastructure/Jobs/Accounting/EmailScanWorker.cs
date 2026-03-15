using MesTech.Infrastructure.Email;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs.Accounting;

/// <summary>
/// E-posta tarama Hangfire worker — her 2 saatte bir calisir.
/// IMAP ile muhasebe e-postalarini tarar, ekleri siniflandirir.
/// MUH-03 DEV 4.
/// </summary>
public class EmailScanWorker : IAccountingJob
{
    public string JobId => "accounting-email-scan";
    public string CronExpression => "0 */2 * * *"; // Her 2 saatte bir

    private readonly IAccountingEmailScanner _emailScanner;
    private readonly ILogger<EmailScanWorker> _logger;

    public EmailScanWorker(
        IAccountingEmailScanner emailScanner,
        ILogger<EmailScanWorker> logger)
    {
        _emailScanner = emailScanner;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] E-posta tarama basliyor...", JobId);

        try
        {
            var count = await _emailScanner.ScanAndProcessAsync(ct);

            _logger.LogInformation(
                "[{JobId}] Email scan complete: {Count} attachments processed",
                JobId, count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "[{JobId}] E-posta tarama HATA", JobId);
            throw;
        }
    }
}
