using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// Expense entity EF Core Fluent API configuration.
/// </summary>
public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.Date })
            .HasDatabaseName("IX_Expenses_Tenant_Date");

        builder.HasIndex(e => new { e.TenantId, e.ExpenseType })
            .HasDatabaseName("IX_Expenses_Tenant_Type");

        builder.HasIndex(e => new { e.TenantId, e.PaymentStatus })
            .HasDatabaseName("IX_Expenses_Tenant_PaymentStatus");

        builder.HasIndex(e => new { e.TenantId, e.SupplierId })
            .HasFilter("\"SupplierId\" IS NOT NULL")
            .HasDatabaseName("IX_Expenses_Tenant_Supplier");

        // String constraints
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.InvoiceNumber).HasMaxLength(50);
        builder.Property(e => e.Note).HasMaxLength(2000);
        builder.Property(e => e.RecurrencePeriod).HasMaxLength(50);

        // Decimal precision
        builder.Property(e => e.Amount).HasPrecision(18, 2);
    }
}
