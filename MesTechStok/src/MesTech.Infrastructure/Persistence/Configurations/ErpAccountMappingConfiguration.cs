using MesTech.Domain.Entities.Erp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ErpAccountMappingConfiguration : IEntityTypeConfiguration<ErpAccountMapping>
{
    public void Configure(EntityTypeBuilder<ErpAccountMapping> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.TenantId, m.MesTechAccountCode })
            .HasDatabaseName("IX_ErpAccountMappings_Tenant_MesTechCode")
            .IsUnique();

        builder.HasIndex(m => new { m.TenantId, m.ErpAccountCode })
            .HasDatabaseName("IX_ErpAccountMappings_Tenant_ErpCode")
            .IsUnique();

        builder.Property(m => m.MesTechAccountCode).HasMaxLength(50).IsRequired();
        builder.Property(m => m.MesTechAccountName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.MesTechAccountType).HasMaxLength(50).IsRequired();
        builder.Property(m => m.ErpAccountCode).HasMaxLength(50).IsRequired();
        builder.Property(m => m.ErpAccountName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Provider).HasConversion<string>().HasMaxLength(30);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
