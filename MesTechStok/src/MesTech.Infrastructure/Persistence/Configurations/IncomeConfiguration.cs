using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// Income entity EF Core Fluent API configuration.
/// </summary>
public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        // Indexes
        builder.HasIndex(i => new { i.TenantId, i.Date })
            .HasDatabaseName("IX_Incomes_Tenant_Date");

        builder.HasIndex(i => new { i.TenantId, i.IncomeType })
            .HasDatabaseName("IX_Incomes_Tenant_Type");

        builder.HasIndex(i => new { i.TenantId, i.StoreId })
            .HasDatabaseName("IX_Incomes_Tenant_Store");

        // String constraints
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.Currency).HasMaxLength(10);
        builder.Property(i => i.Note).HasMaxLength(2000);

        // Decimal precision
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.CommissionAmount).HasPrecision(18, 2);
        builder.Property(i => i.ShippingCost).HasPrecision(18, 2);
    }
}
