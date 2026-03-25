using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class LegalEntityConfiguration : IEntityTypeConfiguration<LegalEntity>
{
    public void Configure(EntityTypeBuilder<LegalEntity> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.TaxNumber })
            .HasDatabaseName("IX_LegalEntities_Tenant_TaxNumber");

        builder.HasIndex(e => new { e.TenantId, e.IsDefault })
            .HasDatabaseName("IX_LegalEntities_Tenant_Default");

        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.TaxNumber).HasMaxLength(11).IsRequired();
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(200);
    }
}
