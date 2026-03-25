namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Sabit kiymet (amortismana tabi iktisadi kiymet) DTO'su.
/// </summary>
public sealed class FixedAssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetCode { get; set; } = string.Empty;
    public decimal AcquisitionCost { get; set; }
    public DateTime AcquisitionDate { get; set; }
    public int UsefulLifeYears { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Amortisman hesaplama sonucu — yillik tablo ve ozet.
/// </summary>
public sealed class DepreciationResultDto
{
    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public decimal AcquisitionCost { get; set; }
    public string Method { get; set; } = string.Empty;
    public int UsefulLifeYears { get; set; }
    public decimal CurrentYearDepreciation { get; set; }
    public List<DepreciationScheduleLineDto> Schedule { get; set; } = new();
}

/// <summary>
/// Amortisman takvimi satiri (Application DTO).
/// </summary>
public sealed class DepreciationScheduleLineDto
{
    public int Year { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
}
