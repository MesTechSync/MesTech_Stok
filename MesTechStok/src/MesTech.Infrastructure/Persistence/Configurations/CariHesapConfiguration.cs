using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// CariHesap entity EF Core Fluent API configuration.
/// </summary>
public class CariHesapConfiguration : IEntityTypeConfiguration<CariHesap>
{
    public void Configure(EntityTypeBuilder<CariHesap> builder)
    {
        // Indexes
        builder.HasIndex(ch => new { ch.TenantId, ch.TaxNumber })
            .HasFilter("\"TaxNumber\" IS NOT NULL AND \"IsDeleted\" = false")
            .HasDatabaseName("IX_CariHesaplar_Tenant_TaxNumber");

        builder.HasIndex(ch => new { ch.TenantId, ch.Type })
            .HasDatabaseName("IX_CariHesaplar_Tenant_Type");

        builder.HasIndex(ch => new { ch.TenantId, ch.Email })
            .HasFilter("\"Email\" IS NOT NULL AND \"IsDeleted\" = false")
            .HasDatabaseName("IX_CariHesaplar_Tenant_Email");

        // String constraints
        builder.Property(ch => ch.Name).HasMaxLength(200).IsRequired();
        builder.Property(ch => ch.TaxNumber).HasMaxLength(50);
        builder.Property(ch => ch.Phone).HasMaxLength(20);
        builder.Property(ch => ch.Email).HasMaxLength(256);
        builder.Property(ch => ch.Address).HasMaxLength(500);

        // Relationships
        builder.HasMany(ch => ch.Hareketler)
            .WithOne(h => h.CariHesap)
            .HasForeignKey(h => h.CariHesapId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
