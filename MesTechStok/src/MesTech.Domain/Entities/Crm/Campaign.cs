using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;

namespace MesTech.Domain.Entities.Crm;

public sealed class Campaign : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public PlatformType? PlatformType { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<CampaignProduct> _products = new();
    public IReadOnlyList<CampaignProduct> Products => _products.AsReadOnly();

    private Campaign() { }

    public static Campaign Create(
        Guid tenantId, string name, DateTime startDate, DateTime endDate,
        decimal discountPercent, PlatformType? platformType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));
        if (discountPercent is <= 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "Discount must be between 0 and 100.");

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            DiscountPercent = discountPercent,
            PlatformType = platformType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        campaign.RaiseDomainEvent(new CampaignCreatedEvent(
            campaign.Id, tenantId, name, discountPercent, platformType,
            startDate, endDate, DateTime.UtcNow));

        return campaign;
    }

    public void AddProduct(CampaignProduct product)
    {
        _products.Add(product);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartDate && now <= EndDate;
    }
}
