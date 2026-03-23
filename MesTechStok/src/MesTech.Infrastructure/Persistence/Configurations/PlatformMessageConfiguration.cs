using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class PlatformMessageConfiguration : IEntityTypeConfiguration<PlatformMessage>
{
    public void Configure(EntityTypeBuilder<PlatformMessage> builder)
    {
        // Indexes
        builder.HasIndex(m => new { m.TenantId, m.Platform, m.ExternalMessageId })
            .IsUnique()
            .HasDatabaseName("IX_PlatformMessages_Tenant_Platform_ExtId");

        builder.HasIndex(m => new { m.TenantId, m.Status })
            .HasDatabaseName("IX_PlatformMessages_Tenant_Status");

        builder.HasIndex(m => new { m.TenantId, m.OrderId })
            .HasFilter("\"OrderId\" IS NOT NULL")
            .HasDatabaseName("IX_PlatformMessages_Tenant_Order");

        // String constraints
        builder.Property(m => m.ExternalMessageId).HasMaxLength(200).IsRequired();
        builder.Property(m => m.ExternalConversationId).HasMaxLength(200);
        builder.Property(m => m.SenderName).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Body).HasMaxLength(8000).IsRequired();
        builder.Property(m => m.AiSuggestedReply).HasMaxLength(4000);
        builder.Property(m => m.Reply).HasMaxLength(4000);
        builder.Property(m => m.RepliedBy).HasMaxLength(100);
    }
}
