namespace MesTech.Domain.Enums;

/// <summary>
/// İade inceleme kalite derecesi (E21).
/// Iade edilen ürünün durumunu belirler.
/// </summary>
public enum ReturnInspectionGrade
{
    /// <summary>A — Mükemmel: Tekrar satılabilir (orijinal ambalaj, hasarsız)</summary>
    A_Resellable = 0,

    /// <summary>B — İyi: Küçük kozmetik hasar, satılabilir (indirimli)</summary>
    B_MinorDamage = 1,

    /// <summary>C — Defolu: Fonksiyonel sorun, tamir gerekli veya parça olarak kullanılabilir</summary>
    C_Defective = 2,

    /// <summary>D — İmha: Kullanılamaz, geri dönüşüme verilmeli</summary>
    D_Dispose = 3
}
