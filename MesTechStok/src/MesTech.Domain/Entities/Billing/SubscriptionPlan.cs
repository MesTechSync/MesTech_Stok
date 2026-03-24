using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Billing;

/// <summary>
/// Abonelik plani — SaaS fiyatlandirma (Baslangic / Profesyonel / Kurumsal).
/// Tenant'lar bir plana abone olur, plan limitleri tenant'in kullanim sinirlarini belirler.
/// </summary>
public class SubscriptionPlan : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public decimal AnnualPrice { get; private set; }
    public string CurrencyCode { get; private set; } = "TRY";
    public int MaxStores { get; private set; }
    public int MaxProducts { get; private set; }
    public int MaxUsers { get; private set; }
    public string? FeaturesJson { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int TrialDays { get; private set; } = 14;
    public int SortOrder { get; private set; }

    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(
        string name, decimal monthlyPrice, decimal annualPrice,
        int maxStores, int maxProducts, int maxUsers,
        string currencyCode = "TRY", int trialDays = 14,
        string? description = null, string? featuresJson = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            MonthlyPrice = monthlyPrice,
            AnnualPrice = annualPrice,
            CurrencyCode = currencyCode,
            MaxStores = maxStores,
            MaxProducts = maxProducts,
            MaxUsers = maxUsers,
            FeaturesJson = featuresJson,
            TrialDays = trialDays,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Baslangic plani seed verisi.</summary>
    public static SubscriptionPlan SeedBasic() => Create(
        "Baslangic", 299m, 2990m, maxStores: 1, maxProducts: 500, maxUsers: 1,
        description: "Tek magaza, temel ozellikler", sortOrder: 1);

    /// <summary>Profesyonel plan seed verisi.</summary>
    public static SubscriptionPlan SeedProfessional() => Create(
        "Profesyonel", 799m, 7990m, maxStores: 5, maxProducts: 10000, maxUsers: 5,
        description: "Coklu magaza, tum ozellikler", sortOrder: 2);

    /// <summary>Kurumsal plan seed verisi.</summary>
    public static SubscriptionPlan SeedEnterprise() => Create(
        "Kurumsal", 1999m, 19990m, maxStores: int.MaxValue, maxProducts: int.MaxValue, maxUsers: int.MaxValue,
        description: "Sinirsiz, oncelikli destek", sortOrder: 3);

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePricing(decimal monthly, decimal annual)
    {
        MonthlyPrice = monthly;
        AnnualPrice = annual;
        UpdatedAt = DateTime.UtcNow;
    }
}
