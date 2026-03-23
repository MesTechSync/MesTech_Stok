using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class LoyaltyProgramConfiguration : IEntityTypeConfiguration<LoyaltyProgram>
{
    public void Configure(EntityTypeBuilder<LoyaltyProgram> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.PointsPerPurchase).HasPrecision(10, 4);

        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_LoyaltyPrograms_Tenant_Active");
    }
}
