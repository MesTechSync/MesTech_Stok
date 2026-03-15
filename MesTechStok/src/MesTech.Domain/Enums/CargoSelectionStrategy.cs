namespace MesTech.Domain.Enums;

/// <summary>
/// Kargo saglayici secim stratejisi.
/// Phase C: AvailabilityFirst (mevcut), CheapestFirst, FastestFirst.
/// </summary>
public enum CargoSelectionStrategy
{
    /// <summary>
    /// Mevcut davranis: oncelik sirasina gore ilk musait olan secilir.
    /// </summary>
    AvailabilityFirst = 0,

    /// <summary>
    /// Tum saglayicilara fiyat sorgusu yapilir, en ucuz secilir.
    /// </summary>
    CheapestFirst = 1,

    /// <summary>
    /// Tum saglayicilara fiyat sorgusu yapilir, en hizli teslimat secilir.
    /// </summary>
    FastestFirst = 2
}
