using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Banking.Parsers;

/// <summary>
/// ISO 20022 camt.053.001.08 (Bank-to-Customer Statement) XML parser.
/// XDocument/LINQ to XML ile &lt;Ntry&gt; elementlerini parse eder.
/// </summary>
public sealed class Camt053Parser : IBankStatementParser
{
    private const string Camt053Namespace = "urn:iso:std:iso:20022:tech:xsd:camt.053.001.08";

    /// <summary>Placeholder tenantId — overwritten by BankStatementImportService.ImportAsync().</summary>
    private static readonly Guid ParserPlaceholderTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly ILogger<Camt053Parser> _logger;

    public string Format => "CAMT053";

    public Camt053Parser(ILogger<Camt053Parser> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<BankTransaction>> ParseAsync(
        Stream data,
        Guid bankAccountId,
        CancellationToken ct = default)
    {
        var doc = await XDocument.LoadAsync(data, LoadOptions.None, ct);
        var transactions = new List<BankTransaction>();

        // Try with namespace, fallback without
        var ns = DetectNamespace(doc);
        var entries = doc.Descendants(ns != null ? ns + "Ntry" : "Ntry").ToList();

        _logger.LogInformation(
            "[Camt053Parser] {EntryCount} Ntry elementi bulundu, bankAccountId={BankAccountId}",
            entries.Count, bankAccountId);

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var transaction = ParseEntry(entry, ns, bankAccountId);
                if (transaction != null)
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Camt053Parser] Ntry elementi parse edilemedi, atlaniyor");
            }
        }

        _logger.LogInformation(
            "[Camt053Parser] {Count} islem basariyla parse edildi", transactions.Count);

        return transactions.AsReadOnly();
    }

    private BankTransaction? ParseEntry(XElement entry, XNamespace? ns, Guid bankAccountId)
    {
        // Amount: <Amt Ccy="TRY">1234.56</Amt>
        var amtElement = entry.Element(ns != null ? ns + "Amt" : "Amt");
        if (amtElement == null)
        {
            _logger.LogWarning("[Camt053Parser] Amt elementi bulunamadi");
            return null;
        }

        var amountStr = amtElement.Value.Trim();
        if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
            || amount < 0 || amount > 999_999_999m)
        {
            _logger.LogWarning("[Camt053Parser] Tutar geçersiz (negatif, overflow veya parse hatası): {AmountStr}", amountStr);
            return null;
        }

        // Credit/Debit indicator: <CdtDbtInd>CRDT</CdtDbtInd> or <CdtDbtInd>DBIT</CdtDbtInd>
        var cdtDbtInd = GetElementValue(entry, "CdtDbtInd", ns);
        if (string.Equals(cdtDbtInd, "DBIT", StringComparison.OrdinalIgnoreCase))
        {
            amount = -Math.Abs(amount);
        }

        // Value Date: <ValDt><Dt>2026-03-15</Dt></ValDt>
        var transactionDate = DateTime.UtcNow;
        var valDtElement = entry.Element(ns != null ? ns + "ValDt" : "ValDt");
        if (valDtElement != null)
        {
            var dtValue = GetElementValue(valDtElement, "Dt", ns);
            if (!string.IsNullOrWhiteSpace(dtValue) &&
                DateTime.TryParse(dtValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
            {
                transactionDate = parsedDate;
            }
        }

        // Fallback to Booking Date if ValDt is not available
        if (valDtElement == null)
        {
            var bookgDtElement = entry.Element(ns != null ? ns + "BookgDt" : "BookgDt");
            if (bookgDtElement != null)
            {
                var dtValue = GetElementValue(bookgDtElement, "Dt", ns);
                if (!string.IsNullOrWhiteSpace(dtValue) &&
                    DateTime.TryParse(dtValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
                {
                    transactionDate = parsedDate;
                }
            }
        }

        // Remittance Information: <RmtInf><Ustrd>Payment description</Ustrd></RmtInf>
        var description = "CAMT053 Transaction";
        var rmtInfElement = entry.Element(ns != null ? ns + "RmtInf" : "RmtInf");
        if (rmtInfElement != null)
        {
            var ustrd = GetElementValue(rmtInfElement, "Ustrd", ns);
            if (!string.IsNullOrWhiteSpace(ustrd))
            {
                description = ustrd;
            }
        }

        // Also check AddtlNtryInf for additional description
        var addtlInfo = GetElementValue(entry, "AddtlNtryInf", ns);
        if (!string.IsNullOrWhiteSpace(addtlInfo) && description == "CAMT053 Transaction")
        {
            description = addtlInfo;
        }

        // Account Servicer Reference: <AcctSvcrRef>REF123</AcctSvcrRef>
        var acctSvcrRef = GetElementValue(entry, "AcctSvcrRef", ns);

        // Idempotency key: use AcctSvcrRef if available, otherwise compute hash
        string idempotencyKey;
        if (!string.IsNullOrWhiteSpace(acctSvcrRef))
        {
            idempotencyKey = ComputeIdempotencyKey(bankAccountId, acctSvcrRef);
        }
        else
        {
            idempotencyKey = ComputeIdempotencyKey(
                bankAccountId,
                transactionDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                amountStr,
                description);
        }

        return BankTransaction.Create(
            tenantId: ParserPlaceholderTenantId, // Overwritten by BankStatementImportService.ImportAsync()
            bankAccountId: bankAccountId,
            transactionDate: transactionDate,
            amount: amount,
            description: description,
            referenceNumber: acctSvcrRef,
            idempotencyKey: idempotencyKey);
    }

    /// <summary>
    /// Namespace'i otomatik tespit eder. camt.053 namespace varsa dondurur.
    /// </summary>
    private static XNamespace? DetectNamespace(XDocument doc)
    {
        var root = doc.Root;
        if (root == null) return null;

        // Direkt camt.053 namespace kontrol
        if (root.Name.Namespace.NamespaceName.Contains("camt.053", StringComparison.OrdinalIgnoreCase))
        {
            return root.Name.Namespace;
        }

        // Alt elementlerde ara
        foreach (var ns in root.DescendantsAndSelf()
                     .Select(e => e.Name.Namespace)
                     .Where(n => n.NamespaceName.Contains("camt.053", StringComparison.OrdinalIgnoreCase))
                     .Take(1))
        {
            return ns;
        }

        // Namespace yoksa null dondur (namespace'siz XML)
        return root.Name.Namespace.NamespaceName == string.Empty ? null : root.Name.Namespace;
    }

    private static string? GetElementValue(XElement parent, string localName, XNamespace? ns)
    {
        var element = parent.Element(ns != null ? ns + localName : localName);
        return element?.Value.Trim();
    }

    /// <summary>
    /// AcctSvcrRef bazli idempotency key.
    /// </summary>
    internal static string ComputeIdempotencyKey(Guid bankAccountId, string reference)
    {
        var input = $"{bankAccountId}|{reference}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Computed hash bazli idempotency key (AcctSvcrRef yoksa).
    /// </summary>
    internal static string ComputeIdempotencyKey(Guid bankAccountId, string date, string amount, string description)
    {
        var input = $"{bankAccountId}|{date}|{amount}|{description}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }
}
