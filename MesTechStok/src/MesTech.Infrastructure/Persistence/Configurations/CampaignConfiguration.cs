using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(300);
        builder.Property(e => e.DiscountPercent).HasPrecision(5, 2);

        builder.HasMany(e => e.Products)
            .WithOne(p => p.Campaign)
            .HasForeignKey(p => p.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_Campaigns_Tenant_Active");

        builder.HasIndex(e => new { e.TenantId, e.StartDate, e.EndDate })
            .HasDatabaseName("IX_Campaigns_Tenant_DateRange");
    }
}
