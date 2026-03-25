using MesTech.Domain.Dropshipping.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// DropshipSupplier entity EF Core Fluent API configuration.
/// </summary>
public sealed class DropshipSupplierConfiguration : IEntityTypeConfiguration<DropshipSupplier>
{
    public void Configure(EntityTypeBuilder<DropshipSupplier> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_DropshipSuppliers_Tenant_Active");

        builder.HasIndex(e => new { e.TenantId, e.Name })
            .HasDatabaseName("IX_DropshipSuppliers_Tenant_Name");

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.WebsiteUrl).HasMaxLength(500);
        builder.Property(e => e.ApiEndpoint).HasMaxLength(500);
        builder.Property(e => e.ApiKey).HasMaxLength(500);
        builder.Property(e => e.MarkupValue).HasPrecision(18, 4);
    }
}
