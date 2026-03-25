using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace MesTech.Infrastructure.Email;

// ── Interface ──

/// <summary>
/// IMAP sunucusundaki muhasebe e-postalarini tarar,
/// eklerdeki belgeleri MESA AI ile siniflandirir ve AccountingDocument olusturur.
/// MUH-03 DEV 4.
/// </summary>
public interface IAccountingEmailScanner
{
    Task<int> ScanAndProcessAsync(CancellationToken ct = default);
}

// ── Implementation ──

/// <summary>
/// MailKit IMAP ile e-posta tarama, ek indirme ve MESA siniflandirma.
/// Config: Email:Accounting:Host, Port, Username, Password, Folder.
/// </summary>
public sealed class AccountingEmailScanner : IAccountingEmailScanner
{
    private readonly IConfiguration _configuration;
    private readonly IMesaAccountingService _accountingService;
    private readonly IAccountingDocumentRepository _documentRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountingEmailScanner> _logger;

    // Desteklenen ek MIME turleri
    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/octet-stream" // Excel dosyalari bazen boyle gelir
    };

    // Dosya uzantisi ile MIME eslestirme (fallback)
    private static readonly Dictionary<string, string> ExtensionToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".tiff"] = "image/tiff",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".xls"] = "application/vnd.ms-excel"
    };

    public AccountingEmailScanner(
        IConfiguration configuration,
        IMesaAccountingService accountingService,
        IAccountingDocumentRepository documentRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<AccountingEmailScanner> logger)
    {
        _configuration = configuration;
        _accountingService = accountingService;
        _documentRepository = documentRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ScanAndProcessAsync(CancellationToken ct = default)
    {
        var host = _configuration["Email:Accounting:Host"];
        var portStr = _configuration["Email:Accounting:Port"];
        var username = _configuration["Email:Accounting:Username"];
        var password = _configuration["Email:Accounting:Password"];
        var folderName = _configuration["Email:Accounting:Folder"] ?? "INBOX";

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "[EmailScanner] IMAP yapilandirmasi eksik (Host/Username/Password). Tarama atlanıyor.");
            return 0;
        }

        var port = int.TryParse(portStr, out var p) ? p : 993;
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var processedCount = 0;

        _logger.LogInformation(
            "[EmailScanner] IMAP tarama basliyor: host={Host}, port={Port}, folder={Folder}",
            host, port, folderName);

        using var client = new ImapClient();

        try
        {
            // 1. IMAP baglantisi
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.Auto, ct);
            await client.AuthenticateAsync(username, password, ct);

            _logger.LogInformation("[EmailScanner] IMAP baglantisi basarili: {Host}", host);

            // 2. Klasor ac
            var folder = await client.GetFolderAsync(folderName, ct);
            await folder.OpenAsync(FolderAccess.ReadWrite, ct);

            // 3. Okunmamis mesajlari bul
            var uids = await folder.SearchAsync(SearchQuery.NotSeen, ct);

            _logger.LogInformation(
                "[EmailScanner] {Count} okunmamis mesaj bulundu", uids.Count);

            if (uids.Count == 0)
            {
                await client.DisconnectAsync(true, ct);
                return 0;
            }

            // 4. Her mesaji isle
            foreach (var uid in uids)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var message = await folder.GetMessageAsync(uid, ct);

                    _logger.LogDebug(
                        "[EmailScanner] Mesaj isleniyor: UID={Uid}, Konu={Subject}",
                        uid, message.Subject ?? "(konu yok)");

                    var attachmentCount = await ProcessAttachmentsAsync(
                        message, tenantId, ct);

                    if (attachmentCount > 0)
                    {
                        processedCount += attachmentCount;

                        // Mesaji okundu olarak isaretle
                        await folder.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);

                        _logger.LogInformation(
                            "[EmailScanner] Mesaj islendi: UID={Uid}, {Count} ek siniflandirildi",
                            uid, attachmentCount);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex,
                        "[EmailScanner] Mesaj isleme hatasi: UID={Uid}. Sonraki mesaja geciliyor.",
                        uid);
                    // Devam et — tek mesaj hatasi tum taramayi durdurmasin
                }
            }

            // 5. Degisiklikleri kaydet
            if (processedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }

            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "[EmailScanner] IMAP tarama genel hatasi: host={Host}", host);

            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true, ct);
                }
            }
            catch (Exception disconnectEx)
            {
                _logger.LogWarning(disconnectEx,
                    "[EmailScanner] IMAP baglanti kapatma hatasi");
            }
        }

        _logger.LogInformation(
            "[EmailScanner] Tarama tamamlandi: {Count} ek islendi", processedCount);

        return processedCount;
    }

    /// <summary>
    /// Mesajdaki ekleri indirir, siniflandirir ve AccountingDocument olusturur.
    /// </summary>
    private async Task<int> ProcessAttachmentsAsync(
        MimeMessage message,
        Guid tenantId,
        CancellationToken ct)
    {
        var count = 0;

        foreach (var attachment in message.Attachments)
        {
            ct.ThrowIfCancellationRequested();

            if (attachment is not MimePart mimePart)
                continue;

            var fileName = mimePart.FileName;
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            // MIME tipi kontrolu
            var mimeType = mimePart.ContentType.MimeType;

            // Fallback: dosya uzantisina gore MIME tipi
            if (!SupportedMimeTypes.Contains(mimeType))
            {
                var ext = Path.GetExtension(fileName);
                if (!ExtensionToMime.TryGetValue(ext, out var fallbackMime))
                {
                    _logger.LogDebug(
                        "[EmailScanner] Desteklenmeyen ek tipi: {FileName} ({MimeType}), atlaniyor",
                        fileName, mimeType);
                    continue;
                }
                mimeType = fallbackMime;
            }

            if (mimePart.Content == null)
            {
                _logger.LogDebug("[EmailScanner] Ek icerigi null, atlaniyor: {FileName}", fileName);
                continue;
            }

            try
            {
                // Eki indir
                using var stream = new MemoryStream();
                await mimePart.Content.DecodeToAsync(stream, ct);
                var fileData = stream.ToArray();

                if (fileData.Length == 0)
                {
                    _logger.LogDebug(
                        "[EmailScanner] Bos ek atlaniyor: {FileName}", fileName);
                    continue;
                }

                // MESA AI ile siniflandir
                var classification = await _accountingService.ClassifyDocumentAsync(
                    fileData, mimeType, ct);

                // DocumentType enum donusumu
                var docType = classification.DocumentType switch
                {
                    "Invoice" => DocumentType.Invoice,
                    "Receipt" => DocumentType.Receipt,
                    "BankStatement" => DocumentType.BankStatement,
                    "Settlement" => DocumentType.Settlement,
                    "Contract" => DocumentType.Contract,
                    _ => DocumentType.Other
                };

                // AccountingDocument olustur (PendingApproval)
                var doc = AccountingDocument.Create(
                    tenantId: tenantId,
                    fileName: fileName,
                    mimeType: mimeType,
                    fileSize: fileData.Length,
                    storagePath: $"email/{tenantId}/{Guid.NewGuid()}/{fileName}",
                    documentType: docType,
                    documentSource: DocumentSource.Email,
                    extractedData: JsonSerializer.Serialize(new
                    {
                        classification.DocumentType,
                        classification.Confidence,
                        EmailSubject = message.Subject,
                        EmailFrom = message.From?.ToString(),
                        EmailDate = message.Date.UtcDateTime
                    }));

                await _documentRepository.AddAsync(doc, ct);
                count++;

                _logger.LogInformation(
                    "[EmailScanner] Ek siniflandirildi: {FileName} → {DocType} (guven={Confidence:P0})",
                    fileName, docType, classification.Confidence);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "[EmailScanner] Ek isleme hatasi: {FileName}", fileName);
                // Devam et — sonraki eki dene
            }
        }

        return count;
    }
}
