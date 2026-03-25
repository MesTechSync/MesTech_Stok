using MesTech.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class DocumentFolderConfiguration : IEntityTypeConfiguration<DocumentFolder>
{
    public void Configure(EntityTypeBuilder<DocumentFolder> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);

        builder.HasIndex(e => new { e.TenantId, e.ParentFolderId })
            .HasDatabaseName("IX_DocumentFolders_Tenant_Parent");

        builder.HasIndex(e => new { e.TenantId, e.Name, e.ParentFolderId })
            .IsUnique()
            .HasDatabaseName("IX_DocumentFolders_Tenant_Name_Parent");
    }
}
