using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PlatformAttributeValueMappingConfiguration
    : IEntityTypeConfiguration<PlatformAttributeValueMapping>
{
    public void Configure(EntityTypeBuilder<PlatformAttributeValueMapping> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.InternalValue).HasMaxLength(200).IsRequired();
        builder.Property(m => m.InternalAttributeName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.PlatformValueName).HasMaxLength(200);

        builder.HasIndex(m => m.TenantId)
            .HasDatabaseName("IX_PlatformAttrValueMap_TenantId");

        builder.HasIndex(m => new { m.TenantId, m.InternalAttributeName, m.InternalValue, m.PlatformType })
            .IsUnique()
            .HasDatabaseName("UX_PlatformAttrValueMap_Internal_Platform");

        builder.HasIndex(m => new { m.TenantId, m.PlatformType, m.PlatformAttributeId })
            .HasDatabaseName("IX_PlatformAttrValueMap_Platform_AttrId");
    }
}
