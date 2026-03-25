using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class FeedImportLogConfiguration : IEntityTypeConfiguration<FeedImportLog>
{
    public void Configure(EntityTypeBuilder<FeedImportLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(l => l.Status)
            .HasConversion<int>();

        // Feed geçmişi sorgusu için en kritik index
        builder.HasIndex(l => new { l.TenantId, l.SupplierFeedId })
            .HasDatabaseName("IX_FeedImportLogs_Tenant_Feed");

        // Zaman bazlı sıralama + filtreleme (son N sync)
        builder.HasIndex(l => new { l.SupplierFeedId, l.StartedAt })
            .HasDatabaseName("IX_FeedImportLogs_Feed_StartedAt");

        // Status bazlı filtreleme (başarısız importları bulmak için)
        builder.HasIndex(l => new { l.TenantId, l.Status })
            .HasDatabaseName("IX_FeedImportLogs_Tenant_Status");

        builder.HasIndex(l => l.TenantId)
            .HasDatabaseName("IX_FeedImportLogs_TenantId");

        // SupplierFeed → FeedImportLogs (1:N)
        // Feed silinirse log kayıtları da silinir (cascade)
        builder.HasOne(l => l.Feed)
            .WithMany()
            .HasForeignKey(l => l.SupplierFeedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
