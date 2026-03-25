using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// ReturnRequestLine entity EF Core Fluent API configuration.
/// </summary>
public sealed class ReturnRequestLineConfiguration : IEntityTypeConfiguration<ReturnRequestLine>
{
    public void Configure(EntityTypeBuilder<ReturnRequestLine> builder)
    {
        // Indexes
        builder.HasIndex(rl => new { rl.TenantId, rl.ReturnRequestId })
            .HasDatabaseName("IX_ReturnRequestLines_Tenant_ReturnRequest");

        builder.HasIndex(rl => new { rl.TenantId, rl.ProductId })
            .HasFilter("\"ProductId\" IS NOT NULL")
            .HasDatabaseName("IX_ReturnRequestLines_Tenant_Product");

        // String constraints
        builder.Property(rl => rl.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(rl => rl.SKU).HasMaxLength(50);
        builder.Property(rl => rl.Barcode).HasMaxLength(50);

        // Decimal precision
        builder.Property(rl => rl.UnitPrice).HasPrecision(18, 2);
        builder.Property(rl => rl.RefundAmount).HasPrecision(18, 2);

        // Relationships
        builder.HasOne(rl => rl.Product)
            .WithMany()
            .HasForeignKey(rl => rl.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
