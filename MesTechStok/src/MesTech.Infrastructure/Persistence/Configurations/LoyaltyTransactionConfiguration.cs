using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Program)
            .WithMany()
            .HasForeignKey(e => e.LoyaltyProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.CustomerId })
            .HasDatabaseName("IX_LoyaltyTransactions_Tenant_Customer");

        builder.HasIndex(e => new { e.TenantId, e.LoyaltyProgramId })
            .HasDatabaseName("IX_LoyaltyTransactions_Tenant_Program");
    }
}
