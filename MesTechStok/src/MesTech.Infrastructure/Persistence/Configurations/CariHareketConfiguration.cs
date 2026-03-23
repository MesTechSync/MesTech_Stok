using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// CariHareket entity EF Core Fluent API configuration.
/// </summary>
public class CariHareketConfiguration : IEntityTypeConfiguration<CariHareket>
{
    public void Configure(EntityTypeBuilder<CariHareket> builder)
    {
        // Indexes
        builder.HasIndex(ch => new { ch.TenantId, ch.CariHesapId, ch.Date })
            .HasDatabaseName("IX_CariHareketler_Tenant_Hesap_Date");

        builder.HasIndex(ch => new { ch.TenantId, ch.Date })
            .HasDatabaseName("IX_CariHareketler_Tenant_Date");

        builder.HasIndex(ch => new { ch.TenantId, ch.Direction })
            .HasDatabaseName("IX_CariHareketler_Tenant_Direction");

        // String constraints
        builder.Property(ch => ch.Description).HasMaxLength(2000).IsRequired();

        // Decimal precision
        builder.Property(ch => ch.Amount).HasPrecision(18, 2);
    }
}
