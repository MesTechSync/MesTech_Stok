namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Kargo etiketi yazdırma için veri transferi nesnesi.
/// PDF/ZPL/PNG 3 format destekler.
/// </summary>
public record ShipmentLabelDto
{
    /// <summary>Kargo takip numarası.</summary>
    public string TrackingNumber { get; init; } = "";

    /// <summary>Kargo firma adı (Yurtiçi, Aras, Sürat vb.).</summary>
    public string CargoProviderName { get; init; } = "";

    /// <summary>Gönderici adı.</summary>
    public string SenderName { get; init; } = "";

    /// <summary>Gönderici adresi.</summary>
    public string SenderAddress { get; init; } = "";

    /// <summary>Gönderici telefon numarası.</summary>
    public string SenderPhone { get; init; } = "";

    /// <summary>Alıcı adı soyadı.</summary>
    public string RecipientName { get; init; } = "";

    /// <summary>Alıcı adresi.</summary>
    public string RecipientAddress { get; init; } = "";

    /// <summary>Alıcı telefon numarası.</summary>
    public string RecipientPhone { get; init; } = "";

    /// <summary>Alıcı il.</summary>
    public string RecipientCity { get; init; } = "";

    /// <summary>Alıcı ilçe.</summary>
    public string RecipientDistrict { get; init; } = "";

    /// <summary>Toplam koli sayısı.</summary>
    public int ParcelCount { get; init; } = 1;

    /// <summary>Mevcut koli numarası.</summary>
    public int CurrentParcel { get; init; } = 1;

    /// <summary>Kargo ağırlığı (kg).</summary>
    public decimal Weight { get; init; }

    /// <summary>Ödeme tipi (Gönderici / Alıcı).</summary>
    public string PaymentType { get; init; } = "Gönderici";

    /// <summary>Kapıda ödeme tutarı (varsa).</summary>
    public decimal? CodAmount { get; init; }

    /// <summary>Barkod verisi.</summary>
    public string BarcodeData { get; init; } = "";

    /// <summary>Sipariş numarası.</summary>
    public string OrderNumber { get; init; } = "";

    /// <summary>Gönderim tarihi.</summary>
    public DateTime ShipDate { get; init; } = DateTime.UtcNow;
}
