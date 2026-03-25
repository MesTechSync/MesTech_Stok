using MesTech.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.FileName).HasMaxLength(300);
        builder.Property(e => e.OriginalFileName).HasMaxLength(300);
        builder.Property(e => e.ContentType).HasMaxLength(100);
        builder.Property(e => e.StoragePath).HasMaxLength(1000);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Tags).HasMaxLength(500);

        builder.HasIndex(e => new { e.TenantId, e.FolderId })
            .HasDatabaseName("IX_Documents_Tenant_Folder");

        builder.HasIndex(e => new { e.TenantId, e.OrderId })
            .HasDatabaseName("IX_Documents_Tenant_Order");

        builder.HasIndex(e => new { e.TenantId, e.UploadedByUserId })
            .HasDatabaseName("IX_Documents_Tenant_Uploader");
    }
}
