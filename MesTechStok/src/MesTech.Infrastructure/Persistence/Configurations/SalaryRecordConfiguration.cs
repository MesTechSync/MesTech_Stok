using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class SalaryRecordConfiguration : IEntityTypeConfiguration<SalaryRecord>
{
    public void Configure(EntityTypeBuilder<SalaryRecord> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.Year, e.Month })
            .HasDatabaseName("IX_SalaryRecords_Tenant_Period");

        builder.HasIndex(e => new { e.TenantId, e.PaymentStatus })
            .HasDatabaseName("IX_SalaryRecords_Tenant_PaymentStatus");

        builder.Property(e => e.EmployeeName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.GrossSalary).HasPrecision(18, 2);
        builder.Property(e => e.SGKEmployer).HasPrecision(18, 2);
        builder.Property(e => e.SGKEmployee).HasPrecision(18, 2);
        builder.Property(e => e.IncomeTax).HasPrecision(18, 2);
        builder.Property(e => e.StampTax).HasPrecision(18, 2);
        builder.Property(e => e.NetSalary).HasPrecision(18, 2);
        builder.Property(e => e.TotalEmployerCost).HasPrecision(18, 2);
    }
}
