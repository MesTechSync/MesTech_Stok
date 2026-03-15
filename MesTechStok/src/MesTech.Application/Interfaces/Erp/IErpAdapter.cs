using MesTech.Application.DTOs.ERP;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// Dalga 11: ERP adapter interface — her ERP saglayicisi (Logo, Netsis, Nebim, Parasut, BizimHesap)
/// bu interface'i implement eder. Siparis, fatura ve hesap bakiye sync islemleri icin standart sozlesme.
///
/// Onceki IERPAdapter (Accounting/) string-based idi.
/// Bu interface enum-based ErpProvider kullaniyor ve ErpSyncResult doner.
/// </summary>
public interface IErpAdapter
{
    /// <summary>Adapter'in destekledigi ERP saglayicisi.</summary>
    ErpProvider Provider { get; }

    /// <summary>
    /// Siparisi ERP'ye senkronize eder.
    /// Basarili ise ErpRef (ERP tarafindaki siparis ID) doner.
    /// </summary>
    Task<ErpSyncResult> SyncOrderAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Faturayi ERP'ye senkronize eder.
    /// Basarili ise ErpRef (ERP tarafindaki fatura ID) doner.
    /// </summary>
    Task<ErpSyncResult> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// ERP'den hesap bakiye bilgilerini ceker.
    /// </summary>
    Task<IReadOnlyList<ErpAccountDto>> GetAccountBalancesAsync(CancellationToken ct = default);

    /// <summary>
    /// ERP API'sine baglanti testi yapar.
    /// Baglanti basarili ise true doner.
    /// </summary>
    Task<bool> PingAsync(CancellationToken ct = default);
}
