using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.Code })
            .HasFilter("\"Code\" IS NOT NULL")
            .HasDatabaseName("IX_ExpenseCategories_Tenant_Code");

        builder.HasIndex(e => new { e.TenantId, e.ParentId })
            .HasDatabaseName("IX_ExpenseCategories_Tenant_Parent");

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(20);

        builder.HasOne(e => e.Parent)
            .WithMany()
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
