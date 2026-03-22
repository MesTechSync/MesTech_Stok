using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Crm;

public class CampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DiscountPercent { get; set; }
    public PlatformType? PlatformType { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class CampaignDiscountResultDto
{
    public decimal OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public string AppliedCampaignName { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
}
