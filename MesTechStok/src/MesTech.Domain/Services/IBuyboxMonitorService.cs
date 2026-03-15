namespace MesTech.Domain.Services;

/// <summary>
/// Buybox izleme servisi arayuzu.
/// Kendi fiyatini rakip fiyatlarla karsilastirarak rekabetcilik analizi yapar.
/// Implementasyon Infrastructure katmaninda olacak (dis veri kaynaklari gerektirir).
/// </summary>
public interface IBuyboxMonitorService
{
    /// <summary>
    /// Belirtilen urun icin buybox analizi yapar.
    /// Kendi fiyat ile rakip fiyatlari karsilastirir ve oneri uretir.
    /// </summary>
    BuyboxAnalysis Analyze(BuyboxInput input);

    /// <summary>
    /// Birden fazla urun icin toplu buybox analizi.
    /// </summary>
    IReadOnlyList<BuyboxAnalysis> AnalyzeBatch(IEnumerable<BuyboxInput> inputs);
}
