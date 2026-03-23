using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class CampaignProductConfiguration : IEntityTypeConfiguration<CampaignProduct>
{
    public void Configure(EntityTypeBuilder<CampaignProduct> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.TenantId, e.CampaignId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_CampaignProducts_Tenant_Campaign_Product");
    }
}
