using MesTech.Domain.Entities.Erp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ErpFieldMappingConfiguration : IEntityTypeConfiguration<ErpFieldMapping>
{
    public void Configure(EntityTypeBuilder<ErpFieldMapping> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ErpType).HasMaxLength(50).IsRequired();
        builder.Property(m => m.MesTechField).HasMaxLength(200).IsRequired();
        builder.Property(m => m.ErpField).HasMaxLength(200).IsRequired();
        builder.Property(m => m.TransformExpression).HasMaxLength(1000);

        builder.HasIndex(m => m.TenantId).HasDatabaseName("IX_ErpFieldMappings_TenantId");
        builder.HasIndex(m => new { m.TenantId, m.ErpType, m.MesTechField })
            .IsUnique()
            .HasDatabaseName("UX_ErpFieldMappings_Tenant_Type_Field");
    }
}
