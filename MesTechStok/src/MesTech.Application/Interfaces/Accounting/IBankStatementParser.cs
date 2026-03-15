using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Banka ekstre dosyasini parse edip BankTransaction listesi ureten arayuz.
/// OFX, MT940, CAMT053 gibi formatlar icin implement edilir.
/// </summary>
public interface IBankStatementParser
{
    /// <summary>
    /// Desteklenen format adi (orn. "OFX", "MT940", "CAMT053").
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Stream icerigini parse ederek BankTransaction listesi uretir.
    /// </summary>
    Task<IReadOnlyList<BankTransaction>> ParseAsync(
        Stream data,
        Guid bankAccountId,
        CancellationToken ct = default);
}

/// <summary>
/// Banka ekstre parser factory — format tespiti ve uygun parser secimi.
/// </summary>
public interface IBankStatementParserFactory
{
    /// <summary>
    /// Format adina gore uygun parser'i dondurur.
    /// </summary>
    IBankStatementParser GetParser(string format);

    /// <summary>
    /// Stream iceriginden format tespiti yapar.
    /// Stream position sifirlanir.
    /// </summary>
    string DetectFormat(Stream data);

    /// <summary>
    /// Desteklenen format listesi.
    /// </summary>
    IReadOnlyList<string> SupportedFormats { get; }
}
