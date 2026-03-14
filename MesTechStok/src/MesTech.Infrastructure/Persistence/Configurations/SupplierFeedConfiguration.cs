using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class SupplierFeedConfiguration : IEntityTypeConfiguration<SupplierFeed>
{
    public void Configure(EntityTypeBuilder<SupplierFeed> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
        builder.Property(f => f.FeedUrl).IsRequired().HasMaxLength(2000);
        builder.Property(f => f.CronExpression).HasMaxLength(100);
        builder.Property(f => f.LastSyncError).HasMaxLength(2000);
        builder.Property(f => f.TargetPlatforms).HasMaxLength(500);

        // ENT-DROP-IMP-SPRINT-D D-07: Şifrelenmiş credential alanı (nullable, base64 AES-256-GCM blob)
        builder.Property(f => f.EncryptedCredential).HasMaxLength(1000);

        builder.Property(f => f.PriceMarkupPercent).HasPrecision(5, 2);
        builder.Property(f => f.PriceMarkupFixed).HasPrecision(18, 2);

        builder.Property(f => f.Format).HasConversion<int>();
        builder.Property(f => f.LastSyncStatus).HasConversion<int>();

        builder.HasIndex(f => new { f.TenantId, f.SupplierId })
            .HasDatabaseName("IX_SupplierFeeds_Tenant_Supplier");

        builder.HasIndex(f => new { f.TenantId, f.IsActive })
            .HasDatabaseName("IX_SupplierFeeds_Tenant_Active");

        builder.HasOne(f => f.Supplier)
            .WithMany()
            .HasForeignKey(f => f.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
