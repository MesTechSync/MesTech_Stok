using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Crm;

public class CampaignProduct : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CampaignId { get; private set; }
    public Guid ProductId { get; private set; }

    public Campaign Campaign { get; private set; } = null!;

    private CampaignProduct() { }

    public static CampaignProduct Create(Guid campaignId, Guid productId)
    {
        return new CampaignProduct
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
