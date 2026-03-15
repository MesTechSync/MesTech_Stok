using MesTech.Domain.Accounting.Entities;

namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mutabakat eslestirme servisi arayuzu.
/// SettlementLine'lar ile BankTransaction'lari karstilastirir,
/// eslestirme sonuclarini ReconciliationMatch kayitlari olarak dondurur.
///
/// Guven skoru (Confidence) 0-1 arasi:
///   0.95-1.00: OrderId tam eslestirme + tutar tam eslestirme
///   0.80-0.94: OrderId tam eslestirme + tutar %1 tolerans icinde
///   0.60-0.79: Tutar eslestirme + tarih 3 gun icinde (OrderId yok)
///   0.60 alti: Manuel inceleme gerekli
/// </summary>
public interface IReconciliationService
{
    /// <summary>
    /// SettlementLine listesi ile BankTransaction listesi arasinda otomatik eslestirme yapar.
    /// Her eslestirme icin bir ReconciliationMatch kaydı olusturur.
    /// </summary>
    /// <param name="tenantId">Kiracı kimliği.</param>
    /// <param name="lines">Hesap kesimi satirlari.</param>
    /// <param name="transactions">Banka hareketleri.</param>
    /// <returns>Eslestirme sonuclari — yuksek guven oncelikli.</returns>
    IReadOnlyList<ReconciliationMatch> Reconcile(
        Guid tenantId,
        IReadOnlyList<SettlementLine> lines,
        IReadOnlyList<BankTransaction> transactions);
}
