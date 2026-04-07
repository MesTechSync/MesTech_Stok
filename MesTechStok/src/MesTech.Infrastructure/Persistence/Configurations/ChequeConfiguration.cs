using MesTech.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ChequeConfiguration : IEntityTypeConfiguration<Cheque>
{
    public void Configure(EntityTypeBuilder<Cheque> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ChequeNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.BankName).HasMaxLength(200);
        builder.Property(c => c.BranchCode).HasMaxLength(20);
        builder.Property(c => c.DrawerName).HasMaxLength(200);
        builder.Property(c => c.EndorserName).HasMaxLength(200);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_Cheques_TenantId");
        builder.HasIndex(c => new { c.TenantId, c.ChequeNumber }).IsUnique()
            .HasDatabaseName("UX_Cheques_Tenant_Number");
        builder.HasIndex(c => new { c.TenantId, c.MaturityDate, c.Status })
            .HasDatabaseName("IX_Cheques_Tenant_Maturity_Status");
    }
}
