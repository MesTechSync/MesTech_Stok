using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class WebhookDeadLetterConfiguration : IEntityTypeConfiguration<WebhookDeadLetter>
{
    public void Configure(EntityTypeBuilder<WebhookDeadLetter> builder)
    {
        builder.ToTable("WebhookDeadLetters");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_WebhookDeadLetters_Tenant_Status");

        builder.HasIndex(x => x.NextRetryAt)
            .HasFilter("\"Status\" = 0")
            .HasDatabaseName("IX_WebhookDeadLetters_NextRetry_Pending");

        builder.Property(x => x.Platform).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RawBody).IsRequired();
        builder.Property(x => x.Signature).HasMaxLength(512);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.ProcessedBy).HasMaxLength(200);
    }
}
