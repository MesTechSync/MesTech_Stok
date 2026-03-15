using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Banking.Parsers;

/// <summary>
/// Open Financial Exchange (OFX) 2.2 format parser.
/// SGML-style OFX dosyalarini string islemleriyle parse eder (XDocument kullanmaz).
/// </summary>
public class OFXParser : IBankStatementParser
{
    private readonly ILogger<OFXParser> _logger;

    public string Format => "OFX";

    public OFXParser(ILogger<OFXParser> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<BankTransaction>> ParseAsync(
        Stream data,
        Guid bankAccountId,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(ct);

        var transactions = new List<BankTransaction>();
        var blocks = ExtractBlocks(content, "STMTTRN");

        _logger.LogInformation(
            "[OFXParser] {BlockCount} STMTTRN blogu bulundu, bankAccountId={BankAccountId}",
            blocks.Count, bankAccountId);

        foreach (var block in blocks)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var transaction = ParseTransaction(block, bankAccountId);
                if (transaction != null)
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[OFXParser] STMTTRN blogu parse edilemedi, atlaniyor");
            }
        }

        _logger.LogInformation(
            "[OFXParser] {Count} islem basariyla parse edildi", transactions.Count);

        return transactions.AsReadOnly();
    }

    private BankTransaction? ParseTransaction(string block, Guid bankAccountId)
    {
        var datePosted = ExtractTagValue(block, "DTPOSTED");
        var amountStr = ExtractTagValue(block, "TRNAMT");
        var name = ExtractTagValue(block, "NAME");
        var memo = ExtractTagValue(block, "MEMO");
        var fitId = ExtractTagValue(block, "FITID");

        if (string.IsNullOrWhiteSpace(datePosted) || string.IsNullOrWhiteSpace(amountStr))
        {
            _logger.LogWarning("[OFXParser] DTPOSTED veya TRNAMT eksik, blok atlaniyor");
            return null;
        }

        var transactionDate = ParseOFXDate(datePosted);
        var amount = decimal.Parse(amountStr, CultureInfo.InvariantCulture);

        var description = !string.IsNullOrWhiteSpace(name) ? name : "OFX Transaction";
        if (!string.IsNullOrWhiteSpace(memo) && memo != name)
        {
            description = $"{description} — {memo}";
        }

        var idempotencyKey = ComputeIdempotencyKey(bankAccountId, datePosted, amountStr, fitId ?? string.Empty);

        return BankTransaction.Create(
            tenantId: Guid.Empty, // Tenant, import servisinde set edilir
            bankAccountId: bankAccountId,
            transactionDate: transactionDate,
            amount: amount,
            description: description,
            referenceNumber: fitId,
            idempotencyKey: idempotencyKey);
    }

    /// <summary>
    /// OFX tarih formatini parse eder: yyyyMMddHHmmss veya yyyyMMdd.
    /// </summary>
    private static DateTime ParseOFXDate(string dateStr)
    {
        // OFX tarihleri: 20260315120000[0:GMT] veya 20260315
        // Timezone bilgisini kaldir
        var bracketIdx = dateStr.IndexOf('[');
        if (bracketIdx > 0)
        {
            dateStr = dateStr[..bracketIdx];
        }

        dateStr = dateStr.Trim();

        if (dateStr.Length >= 14)
        {
            return DateTime.ParseExact(
                dateStr[..14], "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        if (dateStr.Length >= 8)
        {
            return DateTime.ParseExact(
                dateStr[..8], "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        throw new FormatException($"OFX date format taninmiyor: {dateStr}");
    }

    /// <summary>
    /// SGML-style OFX'ten belirtilen tag arasindaki bloklari cikarir.
    /// </summary>
    internal static List<string> ExtractBlocks(string content, string tagName)
    {
        var blocks = new List<string>();
        var openTag = $"<{tagName}>";
        var closeTag = $"</{tagName}>";
        var startIdx = 0;

        while (true)
        {
            var begin = content.IndexOf(openTag, startIdx, StringComparison.OrdinalIgnoreCase);
            if (begin < 0) break;

            var end = content.IndexOf(closeTag, begin + openTag.Length, StringComparison.OrdinalIgnoreCase);
            if (end < 0)
            {
                // SGML-style: kapanma etiketi olmayabilir, sonraki acilma etiketine kadar al
                var nextOpen = content.IndexOf(openTag, begin + openTag.Length, StringComparison.OrdinalIgnoreCase);
                end = nextOpen > 0 ? nextOpen : content.Length;
                blocks.Add(content[begin..end]);
                startIdx = end;
            }
            else
            {
                blocks.Add(content[(begin + openTag.Length)..end]);
                startIdx = end + closeTag.Length;
            }
        }

        return blocks;
    }

    /// <summary>
    /// SGML-style tag degerini cikarir.
    /// Ornek: &lt;NAME&gt;MARKET XYZ → "MARKET XYZ"
    /// </summary>
    internal static string? ExtractTagValue(string content, string tagName)
    {
        var tag = $"<{tagName}>";
        var idx = content.IndexOf(tag, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var valueStart = idx + tag.Length;

        // Deger, satir sonu veya sonraki tag'e kadar devam eder
        var nextTagIdx = content.IndexOf('<', valueStart);
        var newLineIdx = content.IndexOfAny(new[] { '\r', '\n' }, valueStart);

        int endIdx;
        if (nextTagIdx >= 0 && newLineIdx >= 0)
            endIdx = Math.Min(nextTagIdx, newLineIdx);
        else if (nextTagIdx >= 0)
            endIdx = nextTagIdx;
        else if (newLineIdx >= 0)
            endIdx = newLineIdx;
        else
            endIdx = content.Length;

        return content[valueStart..endIdx].Trim();
    }

    /// <summary>
    /// SHA256 tabanli idempotency key uretir.
    /// </summary>
    internal static string ComputeIdempotencyKey(Guid bankAccountId, string datePosted, string amount, string fitId)
    {
        var input = $"{bankAccountId}|{datePosted}|{amount}|{fitId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }
}
