using MesTech.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.EmployeeCode).HasMaxLength(50);
        builder.Property(e => e.JobTitle).HasMaxLength(200);
        builder.Property(e => e.WorkEmail).HasMaxLength(254);
        builder.Property(e => e.WorkPhone).HasMaxLength(20);
        builder.Property(e => e.MonthlySalary).HasPrecision(18, 4);
        builder.Property(e => e.HourlyRate).HasPrecision(18, 4);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeCode })
            .IsUnique()
            .HasDatabaseName("IX_Employees_Tenant_Code");

        builder.HasIndex(e => new { e.TenantId, e.DepartmentId })
            .HasDatabaseName("IX_Employees_Tenant_Department");

        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_Employees_Tenant_User");
    }
}
