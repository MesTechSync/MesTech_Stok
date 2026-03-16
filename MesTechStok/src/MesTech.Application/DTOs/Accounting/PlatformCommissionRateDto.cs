namespace MesTech.Application.DTOs.Accounting;

public class PlatformCommissionRateDto
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string CommissionType { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? PlatformCategoryId { get; set; }
    public decimal Rate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
