using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// Warehouse entity EF Core Fluent API configuration.
/// </summary>
public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        // Indexes
        builder.HasIndex(w => new { w.TenantId, w.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Warehouses_Tenant_Code");

        builder.HasIndex(w => new { w.TenantId, w.IsActive })
            .HasDatabaseName("IX_Warehouses_Tenant_Active");

        // String constraints
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Code).HasMaxLength(50).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(2000);
        builder.Property(w => w.Type).HasMaxLength(50);
        builder.Property(w => w.Address).HasMaxLength(500);
        builder.Property(w => w.City).HasMaxLength(100);
        builder.Property(w => w.State).HasMaxLength(100);
        builder.Property(w => w.PostalCode).HasMaxLength(20);
        builder.Property(w => w.Country).HasMaxLength(100);
        builder.Property(w => w.ContactPerson).HasMaxLength(200);
        builder.Property(w => w.Email).HasMaxLength(256);
        builder.Property(w => w.Phone).HasMaxLength(20);
        builder.Property(w => w.CapacityUnit).HasMaxLength(20);
        builder.Property(w => w.OperatingHours).HasMaxLength(200);
        builder.Property(w => w.CostCenter).HasMaxLength(50);
        builder.Property(w => w.Notes).HasMaxLength(2000);

        // Decimal precision
        builder.Property(w => w.TotalArea).HasPrecision(18, 2);
        builder.Property(w => w.UsableArea).HasPrecision(18, 2);
        builder.Property(w => w.Height).HasPrecision(18, 2);
        builder.Property(w => w.MaxCapacity).HasPrecision(18, 2);
        builder.Property(w => w.MinTemperature).HasPrecision(5, 2);
        builder.Property(w => w.MaxTemperature).HasPrecision(5, 2);
        builder.Property(w => w.MinHumidity).HasPrecision(5, 2);
        builder.Property(w => w.MaxHumidity).HasPrecision(5, 2);
        builder.Property(w => w.MonthlyCost).HasPrecision(18, 2);
        builder.Property(w => w.CostPerSquareMeter).HasPrecision(18, 2);
    }
}
