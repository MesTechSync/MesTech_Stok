using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Banking.Parsers;

/// <summary>
/// CSV banka ekstre parser.
/// Tarih, Aciklama, Tutar, Referans kolonlari ile calisan, kolon mapping konfigurasyon destekli parser.
/// Turk bankalari icin virgul decimal separator handling mevcut.
/// </summary>
public sealed class CsvStatementParser : IBankStatementParser
{
    /// <summary>Placeholder tenantId — overwritten by BankStatementImportService.ImportAsync().</summary>
    private static readonly Guid ParserPlaceholderTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly ILogger<CsvStatementParser> _logger;

    public string Format => "CSV";

    /// <summary>
    /// Default kolon mapping. Turk bankalari icin yaygin baslik isimleri desteklenir.
    /// </summary>
    private static readonly Dictionary<string, string[]> DefaultColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Date"] = ["Date", "Tarih", "IslemTarihi", "Islem Tarihi", "TransactionDate", "Valör"],
        ["Description"] = ["Description", "Aciklama", "Açıklama", "Memo", "Detail", "Detay"],
        ["Amount"] = ["Amount", "Tutar", "Miktar", "TRNAMT", "Islem Tutari"],
        ["Reference"] = ["Reference", "Referans", "Ref", "ReferenceNumber", "FITID", "Dekont No"]
    };

    /// <summary>
    /// Turkish date formats commonly used by banks.
    /// </summary>
    private static readonly string[] TurkishDateFormats =
    [
        "dd.MM.yyyy",
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyy HH:mm:ss",
        "yyyy-MM-dd",
        "yyyy-MM-dd HH:mm:ss",
        "MM/dd/yyyy",
        "d.M.yyyy"
    ];

    public CsvStatementParser(ILogger<CsvStatementParser> logger)
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
        var lines = content.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count < 2)
        {
            _logger.LogWarning("[CsvStatementParser] Yeterli satir yok (baslik + en az 1 veri satiri gerekli)");
            return transactions.AsReadOnly();
        }

        // Detect delimiter: semicolon (Garanti, Akbank), comma, or tab
        var delimiter = DetectDelimiter(lines[0]);
        _logger.LogInformation("[CsvStatementParser] Delimiter tespit edildi: '{Delimiter}'", delimiter);

        // Parse header and build column mapping
        var headers = SplitCsvLine(lines[0], delimiter);
        var columnMap = BuildColumnMap(headers);

        if (!columnMap.ContainsKey("Date") || !columnMap.ContainsKey("Amount"))
        {
            _logger.LogWarning("[CsvStatementParser] Zorunlu kolonlar (Date, Amount) bulunamadi. Headers: {Headers}",
                string.Join(", ", headers));
            return transactions.AsReadOnly();
        }

        _logger.LogInformation(
            "[CsvStatementParser] {LineCount} veri satiri islenecek, bankAccountId={BankAccountId}",
            lines.Count - 1, bankAccountId);

        for (var i = 1; i < lines.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var fields = SplitCsvLine(lines[i], delimiter);
                var transaction = ParseRow(fields, columnMap, bankAccountId, i + 1);
                if (transaction != null)
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CsvStatementParser] Satir {Row} parse edilemedi, atlaniyor", i + 1);
            }
        }

        _logger.LogInformation(
            "[CsvStatementParser] {Count} islem basariyla parse edildi", transactions.Count);

        return transactions.AsReadOnly();
    }

    private BankTransaction? ParseRow(
        string[] fields,
        Dictionary<string, int> columnMap,
        Guid bankAccountId,
        int rowNumber)
    {
        // Date
        var dateStr = GetField(fields, columnMap, "Date");
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            _logger.LogWarning("[CsvStatementParser] Satir {Row}: Tarih bos", rowNumber);
            return null;
        }

        var transactionDate = ParseDate(dateStr);
        if (transactionDate == null)
        {
            _logger.LogWarning("[CsvStatementParser] Satir {Row}: Tarih parse edilemedi: {DateStr}",
                rowNumber, dateStr);
            return null;
        }

        // Amount — handle Turkish decimal separator (comma)
        var amountStr = GetField(fields, columnMap, "Amount");
        if (string.IsNullOrWhiteSpace(amountStr))
        {
            _logger.LogWarning("[CsvStatementParser] Satir {Row}: Tutar bos", rowNumber);
            return null;
        }

        var amount = ParseTurkishDecimal(amountStr);
        if (amount == null)
        {
            _logger.LogWarning("[CsvStatementParser] Satir {Row}: Tutar parse edilemedi: {AmountStr}",
                rowNumber, amountStr);
            return null;
        }

        // Description
        var description = GetField(fields, columnMap, "Description");
        if (string.IsNullOrWhiteSpace(description))
        {
            description = "CSV Transaction";
        }

        // Reference
        var reference = GetField(fields, columnMap, "Reference");

        var idempotencyKey = ComputeIdempotencyKey(
            bankAccountId,
            transactionDate.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            amount.Value.ToString(CultureInfo.InvariantCulture),
            reference ?? description);

        return BankTransaction.Create(
            tenantId: ParserPlaceholderTenantId, // Overwritten by BankStatementImportService.ImportAsync()
            bankAccountId: bankAccountId,
            transactionDate: transactionDate.Value,
            amount: amount.Value,
            description: description,
            referenceNumber: reference,
            idempotencyKey: idempotencyKey);
    }

    /// <summary>
    /// CSV delimiter tespit eder: noktalı virgul > tab > virgul.
    /// Turk bankalari genelde noktali virgul kullanir (tutar virgul icerdiginden).
    /// </summary>
    internal static char DetectDelimiter(string headerLine)
    {
        var semicolonCount = headerLine.Count(c => c == ';');
        var tabCount = headerLine.Count(c => c == '\t');
        var commaCount = headerLine.Count(c => c == ',');

        if (semicolonCount > 0 && semicolonCount >= commaCount)
            return ';';
        if (tabCount > 0 && tabCount >= commaCount)
            return '\t';
        return ',';
    }

    /// <summary>
    /// CSV satirini parse eder. Quoted field destegi mevcut.
    /// </summary>
    internal static string[] SplitCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++; // Skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                fields.Add(field.ToString().Trim());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString().Trim());
        return fields.ToArray();
    }

    /// <summary>
    /// Header satirindan kolon mapping olusturur.
    /// Alias destegi: "Tarih" → Date, "Aciklama" → Description vb.
    /// </summary>
    private static Dictionary<string, int> BuildColumnMap(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim().Trim('"');
            foreach (var (logicalName, aliases) in DefaultColumnAliases)
            {
                if (map.ContainsKey(logicalName))
                    continue;

                foreach (var alias in aliases)
                {
                    if (string.Equals(header, alias, StringComparison.OrdinalIgnoreCase))
                    {
                        map[logicalName] = i;
                        break;
                    }
                }
            }
        }

        return map;
    }

    private static string? GetField(string[] fields, Dictionary<string, int> columnMap, string logicalName)
    {
        if (!columnMap.TryGetValue(logicalName, out var idx) || idx >= fields.Length)
            return null;

        var value = fields[idx].Trim().Trim('"');
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Turk decimal formatini parse eder.
    /// "1.234,56" → 1234.56 (binlik ayirici nokta, ondalik virgul)
    /// "1234.56" → 1234.56 (standard)
    /// "-1.234,56" → -1234.56
    /// </summary>
    internal static decimal? ParseTurkishDecimal(string amountStr)
    {
        if (string.IsNullOrWhiteSpace(amountStr))
            return null;

        amountStr = amountStr.Trim().Replace(" ", "");

        // Remove currency symbols
        amountStr = amountStr.Replace("TL", "").Replace("₺", "").Replace("TRY", "").Trim();

        // Detect Turkish format: has comma as decimal separator
        // Pattern: digits.digits.digits,digits (Turkish) vs digits,digits,digits.digits (US)
        if (amountStr.Contains(','))
        {
            var lastComma = amountStr.LastIndexOf(',');
            var lastDot = amountStr.LastIndexOf('.');

            if (lastComma > lastDot)
            {
                // Turkish format: 1.234,56 → remove dots, replace comma with dot
                amountStr = amountStr.Replace(".", "").Replace(',', '.');
            }
            // else: US format (1,234.56) — just remove commas
            else
            {
                amountStr = amountStr.Replace(",", "");
            }
        }

        return decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    /// <summary>
    /// Coklu tarih formati ile parse eder.
    /// </summary>
    private static DateTime? ParseDate(string dateStr)
    {
        dateStr = dateStr.Trim().Trim('"');

        if (DateTime.TryParseExact(dateStr, TurkishDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var result))
        {
            return result;
        }

        // Fallback: generic DateTime.TryParse
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
        {
            return result;
        }

        // Turkish culture fallback
        if (DateTime.TryParse(dateStr, new CultureInfo("tr-TR"),
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// SHA256 tabanli idempotency key uretir.
    /// </summary>
    internal static string ComputeIdempotencyKey(Guid bankAccountId, string date, string amount, string reference)
    {
        var input = $"{bankAccountId}|{date}|{amount}|{reference}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }
}
