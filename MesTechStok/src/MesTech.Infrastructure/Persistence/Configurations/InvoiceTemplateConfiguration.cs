using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class InvoiceTemplateConfiguration : IEntityTypeConfiguration<InvoiceTemplate>
{
    public void Configure(EntityTypeBuilder<InvoiceTemplate> builder)
    {
        // Indexes
        builder.HasIndex(t => new { t.TenantId, t.StoreId, t.IsDefault })
            .HasDatabaseName("IX_InvoiceTemplates_Tenant_Store_Default");

        // String constraints
        builder.Property(t => t.TemplateName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.PhoneNumber).HasMaxLength(30);
        builder.Property(t => t.Email).HasMaxLength(200);
        builder.Property(t => t.TicaretSicilNo).HasMaxLength(50);

        // Blob columns — max 500KB
        builder.Property(t => t.LogoImage).HasMaxLength(512_000);
        builder.Property(t => t.SignatureImage).HasMaxLength(512_000);

        // Navigation
        builder.HasOne(t => t.Store)
            .WithMany()
            .HasForeignKey(t => t.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
