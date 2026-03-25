using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class Bitrix24ContactConfiguration : IEntityTypeConfiguration<Bitrix24Contact>
{
    public void Configure(EntityTypeBuilder<Bitrix24Contact> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ExternalContactId).HasMaxLength(100);
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.LastName).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Email).HasMaxLength(320);
        builder.Property(c => c.CompanyTitle).HasMaxLength(500);

        builder.HasIndex(c => c.CustomerId);
        builder.HasIndex(c => c.ExternalContactId);
        builder.HasIndex(c => c.TenantId);
    }
}
