using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CounterpartyConfiguration : IEntityTypeConfiguration<Counterparty>
{
    public void Configure(EntityTypeBuilder<Counterparty> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.CounterpartyType })
            .HasDatabaseName("IX_Counterparties_Tenant_Type");

        builder.HasIndex(e => new { e.TenantId, e.VKN })
            .HasFilter("\"VKN\" IS NOT NULL")
            .HasDatabaseName("IX_Counterparties_Tenant_VKN");

        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.VKN).HasMaxLength(11);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.Platform).HasMaxLength(50);
    }
}
