using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class VatDeclarationConfiguration : IEntityTypeConfiguration<VatDeclaration>
{
    public void Configure(EntityTypeBuilder<VatDeclaration> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.TotalSales).HasPrecision(18, 2);
        builder.Property(v => v.TotalVatCollected).HasPrecision(18, 2);
        builder.Property(v => v.TotalVatPaid).HasPrecision(18, 2);
        builder.Property(v => v.NetVatPayable).HasPrecision(18, 2);
        builder.Property(v => v.GibReferenceNumber).HasMaxLength(100);
        builder.Property(v => v.Notes).HasMaxLength(2000);

        builder.HasIndex(v => v.TenantId).HasDatabaseName("IX_VatDeclarations_TenantId");
        builder.HasIndex(v => new { v.TenantId, v.Year, v.Month })
            .IsUnique()
            .HasDatabaseName("UX_VatDeclarations_Tenant_Period");
    }
}
