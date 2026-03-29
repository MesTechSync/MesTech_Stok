using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class BackupEntryConfiguration : IEntityTypeConfiguration<BackupEntry>
{
    public void Configure(EntityTypeBuilder<BackupEntry> builder)
    {
        builder.HasKey(b => b.Id);

        builder.HasIndex(b => new { b.TenantId, b.CreatedAt })
            .HasDatabaseName("IX_BackupEntries_Tenant_Created")
            .IsDescending(false, true);

        builder.Property(b => b.FileName).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Status).HasMaxLength(20).IsRequired();
        builder.Property(b => b.ErrorMessage).HasMaxLength(2000);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
