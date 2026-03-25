using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform komisyon oranı — kategori bazlı.
/// Her platform+kategori çifti için komisyon oranı tanımlanır.
/// Sipariş tamamlandığında ilgili oran üzerinden komisyon hesaplanır.
/// </summary>
public sealed class PlatformCommission : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public PlatformType Platform { get; set; }
    public CommissionType Type { get; set; } = CommissionType.Percentage;

    public string? CategoryName { get; set; }
    public string? PlatformCategoryId { get; set; }

    public decimal Rate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string Currency { get; set; } = "TRY";

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public bool IsEffective(DateTime date) =>
        IsActive && date >= EffectiveFrom && (!EffectiveTo.HasValue || date <= EffectiveTo.Value);

    /// <summary>
    /// Komisyon tutarını hesaplar.
    /// </summary>
    public decimal Calculate(decimal saleAmount)
    {
        var commission = Type switch
        {
            CommissionType.Percentage => saleAmount * Rate / 100m,
            CommissionType.FixedAmount => Rate,
            CommissionType.Tiered => saleAmount * Rate / 100m,
            _ => 0m
        };

        if (MinAmount.HasValue && commission < MinAmount.Value)
            commission = MinAmount.Value;
        if (MaxAmount.HasValue && commission > MaxAmount.Value)
            commission = MaxAmount.Value;

        return Math.Round(commission, 2);
    }

    public override string ToString() =>
        $"{Platform} [{CategoryName ?? "Genel"}] %{Rate:N2} ({(IsActive ? "Aktif" : "Pasif")})";
}
