using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Depo Aggregate Root.
/// </summary>
public sealed class Warehouse : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "MAIN";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Fiziksel özellikler
    public decimal? TotalArea { get; set; }
    public decimal? UsableArea { get; set; }
    public decimal? Height { get; set; }
    public decimal? MaxCapacity { get; set; }
    public string? CapacityUnit { get; set; }

    // İklim kontrolü
    public decimal? MinTemperature { get; set; }
    public decimal? MaxTemperature { get; set; }
    public decimal? MinHumidity { get; set; }
    public decimal? MaxHumidity { get; set; }
    public bool HasClimateControl { get; set; }

    // Güvenlik/Altyapı
    public bool HasSecuritySystem { get; set; }
    public bool HasFireProtection { get; set; }
    public bool HasLoadingDock { get; set; }
    public bool HasRacking { get; set; }
    public bool HasForklift { get; set; }

    // Operasyon
    public string? OperatingHours { get; set; }
    public bool Is24Hours { get; set; }
    public decimal? MonthlyCost { get; private set; }
    public decimal? CostPerSquareMeter { get; private set; }
    public string? CostCenter { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; private set; }
    public string? Notes { get; set; }

    // Navigation
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    // ── Domain Logic ──

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public void UnsetDefault()
    {
        IsDefault = false;
    }

    public void UpdateCosts(decimal? monthlyCost, decimal? costPerSquareMeter)
    {
        if (monthlyCost < 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyCost), "Aylık maliyet negatif olamaz.");
        if (costPerSquareMeter < 0)
            throw new ArgumentOutOfRangeException(nameof(costPerSquareMeter), "Birim maliyet negatif olamaz.");
        MonthlyCost = monthlyCost;
        CostPerSquareMeter = costPerSquareMeter;
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Code) ? Name : $"[{Code}] {Name}";

    public override string ToString() => DisplayName;
}
