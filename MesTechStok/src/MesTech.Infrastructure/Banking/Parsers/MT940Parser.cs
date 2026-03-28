using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Banking.Parsers;

/// <summary>
/// SWIFT MT940 Customer Statement Message parser.
/// Tag :61: (Statement Line) ve :86: (Information) satirlarini parse eder.
/// </summary>
public partial class MT940Parser : IBankStatementParser
{
    private readonly ILogger<MT940Parser> _logger;

    public string Format => "MT940";

    public MT940Parser(ILogger<MT940Parser> logger)
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
        var lines = content.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        // Extract transaction reference from :20: tag
        var transactionRef = string.Empty;
        var i = 0;

        while (i < lines.Count)
        {
            ct.ThrowIfCancellationRequested();
            var line = lines[i];

            if (line.StartsWith(":20:", StringComparison.Ordinal))
            {
                transactionRef = line[4..].Trim();
            }
            else if (line.StartsWith(":61:", StringComparison.Ordinal))
            {
                try
                {
                    // Collect any continuation lines for :86: tag
                    var statementLine = line[4..];
                    var description = string.Empty;

                    // Look ahead for :86: tag (information to account owner)
                    if (i + 1 < lines.Count && lines[i + 1].StartsWith(":86:", StringComparison.Ordinal))
                    {
                        i++;
                        description = lines[i][4..].Trim();

                        // Handle multi-line :86: content (continuation lines don't start with a tag)
                        while (i + 1 < lines.Count &&
                               !lines[i + 1].StartsWith(":", StringComparison.Ordinal) &&
                               !string.IsNullOrWhiteSpace(lines[i + 1]))
                        {
                            i++;
                            description += " " + lines[i].Trim();
                        }
                    }

                    var transaction = ParseStatementLine(statementLine, description, transactionRef, bankAccountId);
                    if (transaction != null)
                    {
                        transactions.Add(transaction);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[MT940Parser] :61: satiri parse edilemedi, atlaniyor: {Line}", line);
                }
            }

            i++;
        }

        _logger.LogInformation(
            "[MT940Parser] {Count} islem parse edildi, bankAccountId={BankAccountId}",
            transactions.Count, bankAccountId);

        return transactions.AsReadOnly();
    }

    /// <summary>
    /// :61: tag icerigini parse eder.
    /// Format: YYMMDD[MMDD]CD[amount]NXXX[reference]
    /// Ornek: 2603150315D1234,56NTRFREF123456//BANK-REF
    /// </summary>
    private BankTransaction? ParseStatementLine(
        string line,
        string description,
        string transactionRef,
        Guid bankAccountId)
    {
        if (line.Length < 16)
        {
            _logger.LogWarning("[MT940Parser] :61: satiri cok kisa: {Line}", line);
            return null;
        }

        // Date: YYMMDD (6 chars)
        var dateStr = line[..6];
        var transactionDate = ParseMT940Date(dateStr);

        // Skip optional entry date MMDD (4 chars) — detect by checking for C/D/RC/RD
        var offset = 6;

        // Check if next 4 chars look like MMDD (optional entry date)
        if (offset + 4 <= line.Length &&
            char.IsDigit(line[offset]) &&
            char.IsDigit(line[offset + 1]) &&
            char.IsDigit(line[offset + 2]) &&
            char.IsDigit(line[offset + 3]))
        {
            offset += 4;
        }

        // Credit/Debit indicator: C, D, RC, RD
        decimal sign = 1m;
        if (offset < line.Length)
        {
            if (line[offset] == 'R' && offset + 1 < line.Length)
            {
                // Reversal: RC = reversal credit, RD = reversal debit
                if (line[offset + 1] == 'D')
                    sign = -1m; // Reversal debit is still negative
                offset += 2;
            }
            else if (line[offset] == 'D')
            {
                sign = -1m;
                offset++;
            }
            else if (line[offset] == 'C')
            {
                sign = 1m;
                offset++;
            }
        }

        // Optional third currency letter
        if (offset < line.Length && char.IsLetter(line[offset]) && line[offset] != 'N' && line[offset] != 'F' && line[offset] != 'S')
        {
            offset++;
        }

        // Amount: digits and comma (European decimal separator) until N/F/S transaction type
        var amountBuilder = new StringBuilder();
        while (offset < line.Length && (char.IsDigit(line[offset]) || line[offset] == ',' || line[offset] == '.'))
        {
            amountBuilder.Append(line[offset]);
            offset++;
        }

        var amountStr = amountBuilder.ToString().Replace(',', '.');
        if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            _logger.LogWarning("[MT940Parser] Tutar parse edilemedi: {AmountStr}", amountStr);
            return null;
        }

        amount *= sign;

        // Transaction type (NXXX or FXXX or SXXX) — 4 chars
        var txnType = string.Empty;
        if (offset < line.Length && (line[offset] == 'N' || line[offset] == 'F' || line[offset] == 'S'))
        {
            var typeEnd = Math.Min(offset + 4, line.Length);
            txnType = line[offset..typeEnd];
            offset = typeEnd;
        }

        // Reference (rest of line)
        var reference = offset < line.Length ? line[offset..].Trim() : transactionRef;

        // Clean up reference — remove bank reference after //
        var bankRefSep = reference.IndexOf("//", StringComparison.Ordinal);
        if (bankRefSep >= 0)
        {
            reference = reference[..bankRefSep].Trim();
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            reference = transactionRef;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            description = $"MT940 {txnType} {reference}".Trim();
        }

        var idempotencyKey = ComputeIdempotencyKey(bankAccountId, dateStr, amountStr, reference);

        return BankTransaction.Create(
            tenantId: Guid.Empty, // Overwritten by BankStatementImportService.ImportAsync()
            bankAccountId: bankAccountId,
            transactionDate: transactionDate,
            amount: amount,
            description: description,
            referenceNumber: reference,
            idempotencyKey: idempotencyKey);
    }

    /// <summary>
    /// MT940 tarihini parse eder: YYMMDD.
    /// </summary>
    private static DateTime ParseMT940Date(string dateStr)
    {
        return DateTime.ParseExact(
            dateStr, "yyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
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
