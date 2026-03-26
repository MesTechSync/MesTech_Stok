using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.RevokedReason)
            .HasMaxLength(256);

        builder.Property(t => t.ReplacedByTokenHash)
            .HasMaxLength(128);

        builder.Property(t => t.IpAddress)
            .HasMaxLength(45);

        builder.Property(t => t.UserAgent)
            .HasMaxLength(512);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_token_hash");

        builder.HasIndex(t => new { t.UserId, t.IsRevoked })
            .HasDatabaseName("ix_refresh_tokens_user_active");

        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("ix_refresh_tokens_expires_at");
    }
}
