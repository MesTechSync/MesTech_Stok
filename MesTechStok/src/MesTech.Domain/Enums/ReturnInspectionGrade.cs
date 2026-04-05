namespace MesTech.Domain.Enums;

/// <summary>
/// İade inceleme kalite derecesi (E21).
/// Iade edilen ürünün durumunu belirler.
/// </summary>
public enum ReturnInspectionGrade
{
    /// <summary>A — Mükemmel: Tekrar satılabilir (orijinal ambalaj, hasarsız)</summary>
    Resellable = 0,

    /// <summary>B — İyi: Küçük kozmetik hasar, satılabilir (indirimli)</summary>
    MinorDamage = 1,

    /// <summary>C — Defolu: Fonksiyonel sorun, tamir gerekli veya parça olarak kullanılabilir</summary>
    Defective = 2,

    /// <summary>D — İmha: Kullanılamaz, geri dönüşüme verilmeli</summary>
    Dispose = 3
}
