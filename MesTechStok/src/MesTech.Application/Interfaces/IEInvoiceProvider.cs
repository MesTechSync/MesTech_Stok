using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Dalga 9 e-Fatura provider kontrati.
/// IInvoiceProvider'dan AYRI — UBL-TR 1.2, kontor, VKN sorgulama destegi.
/// Mevcut 9 provider'a dokunmaz.
/// </summary>
public interface IEInvoiceProvider
{
    string ProviderCode { get; }
    Task<EInvoiceSendResult> SendAsync(EInvoiceDocument document, CancellationToken ct = default);
    Task<string?> GetPdfUrlAsync(string providerRef, CancellationToken ct = default);
    Task<bool> CancelAsync(string providerRef, string reason, CancellationToken ct = default);
    Task<VknMukellefResult> CheckVknMukellefAsync(string vkn, CancellationToken ct = default);
    Task<int> GetCreditBalanceAsync(CancellationToken ct = default);
}

public record EInvoiceSendResult(
    bool Success,
    string? ProviderRef,
    string? ErrorMessage,
    int CreditUsed);

public record VknMukellefResult(
    string Vkn,
    bool IsEInvoiceMukellef,
    bool IsEArchiveMukellef,
    string? Title,
    DateTime? CheckedAt);
