using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Amortismana tabi iktisadi kiymet (sabit kiymet) — VUK md. 313-321.
/// Tekduzen Hesap Plani: 253 Tesis/Makine/Cihaz, 254 Tasitlar, 255 Demirbaslar.
/// Amortisman hesabinda (1/Omur)*2 oranli cift azalan veya esit payli yontem kullanilir.
/// </summary>
public class FixedAsset : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Varlik adi (ornegin "CNC Tezgahi", "Ford Transit").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>VUK hesap kodu (253, 254, 255 vb.).</summary>
    public string AssetCode { get; private set; } = string.Empty;

    /// <summary>Satin alma maliyeti (KDV haric).</summary>
    public decimal AcquisitionCost { get; private set; }

    /// <summary>Satin alma / aktife alinma tarihi.</summary>
    public DateTime AcquisitionDate { get; private set; }

    /// <summary>VUK faydali omur (yil) — Maliye Bakanligi listesinden.</summary>
    public int UsefulLifeYears { get; private set; }

    /// <summary>Amortisman yontemi: Normal veya Azalan Bakiyeler.</summary>
    public DepreciationMethod Method { get; private set; }

    /// <summary>Bugunedek birikmis amortisman tutari.</summary>
    public decimal AccumulatedDepreciation { get; private set; }

    /// <summary>Net defter degeri = Maliyet - Birikmis Amortisman.</summary>
    public decimal NetBookValue => AcquisitionCost - AccumulatedDepreciation;

    /// <summary>Varlik aktif mi yoksa hurda/satis ile cikis mi yapildi.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Aciklama / notlar.</summary>
    public string? Description { get; private set; }

    private FixedAsset() { }

    public static FixedAsset Create(
        Guid tenantId,
        string name,
        string assetCode,
        decimal acquisitionCost,
        DateTime acquisitionDate,
        int usefulLifeYears,
        DepreciationMethod method,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetCode);

        if (acquisitionCost <= 0)
            throw new ArgumentOutOfRangeException(nameof(acquisitionCost), "Maliyet sifirdan buyuk olmalidir.");
        if (usefulLifeYears <= 0)
            throw new ArgumentOutOfRangeException(nameof(usefulLifeYears), "Faydali omur sifirdan buyuk olmalidir.");

        var asset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            AssetCode = assetCode,
            AcquisitionCost = acquisitionCost,
            AcquisitionDate = acquisitionDate,
            UsefulLifeYears = usefulLifeYears,
            Method = method,
            AccumulatedDepreciation = 0m,
            IsActive = true,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        asset.RaiseDomainEvent(new FixedAssetCreatedEvent
        {
            TenantId = tenantId,
            FixedAssetId = asset.Id,
            AssetName = name,
            AssetCode = assetCode,
            AcquisitionCost = acquisitionCost,
            Method = method
        });

        return asset;
    }

    /// <summary>
    /// Yillik amortisman kaydi sonrasi birikmis tutari gunceller.
    /// </summary>
    public void ApplyDepreciation(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amortisman tutari pozitif olmalidir.");
        if (AccumulatedDepreciation + amount > AcquisitionCost)
            throw new InvalidOperationException("Birikmis amortisman maliyeti asamaz.");

        AccumulatedDepreciation += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Varligi pasife alir (hurda, satis, devir).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
