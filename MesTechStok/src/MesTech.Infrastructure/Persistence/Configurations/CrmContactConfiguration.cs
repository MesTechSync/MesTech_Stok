using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class CrmContactConfiguration : IEntityTypeConfiguration<CrmContact>
{
    public void Configure(EntityTypeBuilder<CrmContact> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.FullName).HasMaxLength(200);
        builder.Property(e => e.Email).HasMaxLength(254);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Company).HasMaxLength(200);
        builder.Property(e => e.TaxNumber).HasMaxLength(20);
        builder.Property(e => e.TaxOffice).HasMaxLength(100);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasMany(e => e.Deals)
            .WithOne(d => d.Contact)
            .HasForeignKey(d => d.CrmContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_CrmContacts_Tenant_Active");

        builder.HasIndex(e => new { e.TenantId, e.Email })
            .HasDatabaseName("IX_CrmContacts_Tenant_Email");

        builder.HasIndex(e => new { e.TenantId, e.CustomerId })
            .HasDatabaseName("IX_CrmContacts_Tenant_Customer");
    }
}
