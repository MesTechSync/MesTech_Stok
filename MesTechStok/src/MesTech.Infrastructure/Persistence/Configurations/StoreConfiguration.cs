using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(s => s.Id);
        builder.Property(s => s.StoreName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ExternalStoreId).HasMaxLength(100);
        builder.HasIndex(s => new { s.TenantId, s.PlatformType, s.StoreName }).IsUnique();

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Stores)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
