using System.Text;
using MesTech.Application.Interfaces.Accounting;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Banking;

/// <summary>
/// Banka ekstre parser factory.
/// Format tespiti (ilk bytes/satir) ve format adina gore parser secimi.
/// </summary>
public class BankStatementParserFactory : IBankStatementParserFactory
{
    private readonly Dictionary<string, IBankStatementParser> _parsers;
    private readonly ILogger<BankStatementParserFactory> _logger;

    public IReadOnlyList<string> SupportedFormats { get; }

    public BankStatementParserFactory(
        IEnumerable<IBankStatementParser> parsers,
        ILogger<BankStatementParserFactory> logger)
    {
        _logger = logger;
        _parsers = new Dictionary<string, IBankStatementParser>(StringComparer.OrdinalIgnoreCase);

        foreach (var parser in parsers)
        {
            _parsers[parser.Format] = parser;
        }

        SupportedFormats = _parsers.Keys.ToList().AsReadOnly();

        _logger.LogInformation(
            "[BankStatementParserFactory] {Count} parser kayitli: {Formats}",
            SupportedFormats.Count, string.Join(", ", SupportedFormats));
    }

    public IBankStatementParser GetParser(string format)
    {
        if (_parsers.TryGetValue(format, out var parser))
        {
            return parser;
        }

        throw new NotSupportedException(
            $"Desteklenmeyen banka ekstre formati: '{format}'. " +
            $"Desteklenen formatlar: {string.Join(", ", SupportedFormats)}");
    }

    public string DetectFormat(Stream data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!data.CanSeek)
        {
            throw new ArgumentException("Stream seekable olmali (format tespiti icin).", nameof(data));
        }

        var originalPosition = data.Position;

        try
        {
            // Read first 4KB for format detection
            var buffer = new byte[4096];
            var bytesRead = data.Read(buffer, 0, buffer.Length);
            var header = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // OFX detection: SGML header or XML-style <OFX>
            if (header.Contains("OFXHEADER", StringComparison.OrdinalIgnoreCase) ||
                header.Contains("<OFX>", StringComparison.OrdinalIgnoreCase) ||
                header.Contains("<OFX ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[BankStatementParserFactory] OFX formati tespit edildi");
                return "OFX";
            }

            // MT940 detection: starts with :20: tag
            if (header.Contains(":20:", StringComparison.Ordinal) &&
                (header.Contains(":60F:", StringComparison.Ordinal) ||
                 header.Contains(":61:", StringComparison.Ordinal)))
            {
                _logger.LogInformation("[BankStatementParserFactory] MT940 formati tespit edildi");
                return "MT940";
            }

            // CAMT053 detection: XML with camt.053 namespace
            if (header.Contains("camt.053", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("[BankStatementParserFactory] CAMT053 formati tespit edildi");
                return "CAMT053";
            }

            // Fallback: check for generic XML with ISO 20022 indicators
            if (header.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
                (header.Contains("BkToCstmrStmt", StringComparison.OrdinalIgnoreCase) ||
                 header.Contains("iso:std:iso:20022", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("[BankStatementParserFactory] CAMT053 formati (XML fallback) tespit edildi");
                return "CAMT053";
            }

            // CSV fallback: check for common CSV headers (delimiter-separated with known column names)
            var firstLine = header.Split('\n')[0].TrimEnd('\r');
            if (firstLine.Contains("Tarih", StringComparison.OrdinalIgnoreCase) ||
                firstLine.Contains("Date", StringComparison.OrdinalIgnoreCase) ||
                (firstLine.Contains(';') && firstLine.Contains("Tutar", StringComparison.OrdinalIgnoreCase)) ||
                (firstLine.Contains(',') && firstLine.Contains("Amount", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("[BankStatementParserFactory] CSV formati tespit edildi");
                return "CSV";
            }

            throw new NotSupportedException(
                "Banka ekstre formati otomatik tespit edilemedi. " +
                $"Desteklenen formatlar: {string.Join(", ", SupportedFormats)}");
        }
        finally
        {
            // Stream pozisyonunu sifirla
            data.Position = originalPosition;
        }
    }
}
