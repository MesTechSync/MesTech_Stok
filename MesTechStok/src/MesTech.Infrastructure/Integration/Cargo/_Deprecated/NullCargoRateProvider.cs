// ═══════════════════════════════════════════════════════════════════
// DEPRECATED — Bu dosya kullanım dışıdır. Üretim kodunda kullanılamaz.
// ───────────────────────────────────────────────────────────────────
// Arşiv Tarihi  : 2026-03-24
// Dalga         : Dalga 11
// Arşivleyen    : DEV 3 (Komutan direktifi ENT-ARSIV-001 ile geri alındı)
// Yerine Geçen  : DesiBasedCargoRateCalculator.cs
//                 + ICargoAdapter → ICargoRateProvider cast pattern
//                 (CargoProviderSelector, GetCargoComparisonHandler)
// Sebep         : 7 kargo adapter (Yurtici, Aras, Surat, MNG, PTT,
//                 HepsiJet, Sendeo) ICargoRateProvider'ı doğrudan
//                 implement etmeye başladı (Dalga 11 Commit 1ac8244e).
//                 CargoProviderFactory + cast pattern standalone
//                 DI registration'ı gereksiz kıldı.
//                 Orijinal commit: 1ac8244e (git rm ile silindi — ihlal)
//                 Bu dosya ENT-ARSIV-001 gereği _Deprecated/'a taşındı.
// Referans      : EMIRNAME_DOSYA_ARSIV_STANDARDI_v1.md
// ═══════════════════════════════════════════════════════════════════

using MesTech.Application.DTOs.Cargo;
using MesTech.Application.Interfaces.Cargo;

namespace MesTech.Infrastructure.Integration.Cargo;

/// <summary>
/// [DEPRECATED] Her zaman null döndüren boş kargo fiyat sağlayıcısı.
/// Gerçek adapter'lar implement edilene kadar DI fallback olarak kullanılırdı.
/// Artık 7 kargo adapter ICargoRateProvider'ı doğrudan implement etmektedir.
/// </summary>
[Obsolete(
    "Replaced by DesiBasedCargoRateCalculator + adapter cast pattern (Dalga 11). " +
    "All 7 cargo adapters now implement ICargoRateProvider directly. " +
    "Archived to _Deprecated/ per ENT-ARSIV-001. Do not use in production code.",
    error: false)]
public class NullCargoRateProvider : ICargoRateProvider
{
    /// <inheritdoc/>
    /// <remarks>Bu implementasyon her zaman null döndürür — üretim için kullanılamaz.</remarks>
    public Task<CargoRateResult?> GetRateAsync(
        ShipmentRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult<CargoRateResult?>(null);
}
