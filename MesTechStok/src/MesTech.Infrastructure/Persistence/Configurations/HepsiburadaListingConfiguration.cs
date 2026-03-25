using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class HepsiburadaListingConfiguration : IEntityTypeConfiguration<HepsiburadaListing>
{
    public void Configure(EntityTypeBuilder<HepsiburadaListing> builder)
    {
        builder.Property(h => h.HepsiburadaSKU).HasMaxLength(100);
        builder.Property(h => h.MerchantSKU).HasMaxLength(100);
        builder.Property(h => h.ListingStatus).HasMaxLength(50);
        builder.Property(h => h.CommissionRate).HasPrecision(18, 2);

        builder.HasIndex(h => new { h.TenantId, h.HepsiburadaSKU })
            .HasDatabaseName("IX_HepsiburadaListings_Tenant_SKU");
    }
}
