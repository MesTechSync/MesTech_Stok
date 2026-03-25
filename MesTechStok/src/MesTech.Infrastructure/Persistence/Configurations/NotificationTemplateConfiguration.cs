using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        // Indexes
        builder.HasIndex(n => new { n.TenantId, n.TemplateName, n.Channel })
            .IsUnique()
            .HasDatabaseName("IX_NotificationTemplates_Tenant_Name_Channel");

        builder.HasIndex(n => new { n.TenantId, n.IsActive })
            .HasDatabaseName("IX_NotificationTemplates_Tenant_Active");

        // String constraints
        builder.Property(n => n.TemplateName).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(4000).IsRequired();
        builder.Property(n => n.Language).HasMaxLength(10);
    }
}
