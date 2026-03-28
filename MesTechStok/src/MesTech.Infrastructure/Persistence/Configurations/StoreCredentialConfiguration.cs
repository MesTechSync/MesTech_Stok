using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class StoreCredentialConfiguration : IEntityTypeConfiguration<StoreCredential>
{
    public void Configure(EntityTypeBuilder<StoreCredential> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Key).IsRequired().HasMaxLength(100);
        builder.Property(c => c.EncryptedValue).IsRequired().HasMaxLength(2000);
        builder.HasIndex(c => new { c.StoreId, c.Key }).IsUnique();

        builder.HasOne(c => c.Store)
            .WithMany(s => s.Credentials)
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.TenantId).HasDatabaseName("ix_store_credentials_tenant_id");
    }
}
