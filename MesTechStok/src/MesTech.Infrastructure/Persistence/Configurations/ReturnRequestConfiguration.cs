using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// ReturnRequest entity EF Core Fluent API configuration.
/// </summary>
public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        // Indexes
        builder.HasIndex(rr => new { rr.TenantId, rr.OrderId })
            .HasDatabaseName("IX_ReturnRequests_Tenant_Order");

        builder.HasIndex(rr => new { rr.TenantId, rr.Status })
            .HasDatabaseName("IX_ReturnRequests_Tenant_Status");

        builder.HasIndex(rr => new { rr.TenantId, rr.Platform })
            .HasDatabaseName("IX_ReturnRequests_Tenant_Platform");

        builder.HasIndex(rr => rr.PlatformReturnId)
            .HasFilter("\"PlatformReturnId\" IS NOT NULL")
            .HasDatabaseName("IX_ReturnRequests_PlatformReturnId");

        builder.HasIndex(rr => new { rr.TenantId, rr.RequestDate })
            .HasDatabaseName("IX_ReturnRequests_Tenant_RequestDate");

        // String constraints
        builder.Property(rr => rr.PlatformReturnId).HasMaxLength(100);
        builder.Property(rr => rr.ReasonDetail).HasMaxLength(2000);
        builder.Property(rr => rr.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(rr => rr.CustomerEmail).HasMaxLength(256);
        builder.Property(rr => rr.CustomerPhone).HasMaxLength(20);
        builder.Property(rr => rr.Currency).HasMaxLength(10);
        builder.Property(rr => rr.TrackingNumber).HasMaxLength(100);
        builder.Property(rr => rr.Notes).HasMaxLength(2000);

        // Decimal precision
        builder.Property(rr => rr.RefundAmount).HasPrecision(18, 2);

        // Relationships
        builder.HasOne(rr => rr.Order)
            .WithMany()
            .HasForeignKey(rr => rr.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rr => rr.Store)
            .WithMany()
            .HasForeignKey(rr => rr.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(rr => rr.Lines)
            .WithOne(l => l.ReturnRequest)
            .HasForeignKey(l => l.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optimistic concurrency — PostgreSQL xmin pattern (SQL Server IsRowVersion yerine)
        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
        builder.Ignore(rr => rr.RowVersion);
    }
}
