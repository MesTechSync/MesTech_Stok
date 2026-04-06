using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ExternalReviewId).HasMaxLength(100);
        builder.Property(r => r.CustomerName).HasMaxLength(200);
        builder.Property(r => r.Comment).HasMaxLength(4000);
        builder.Property(r => r.ReplyText).HasMaxLength(4000);

        builder.HasIndex(r => r.TenantId).HasDatabaseName("IX_ProductReviews_TenantId");
        builder.HasIndex(r => new { r.TenantId, r.ExternalReviewId, r.Platform })
            .IsUnique()
            .HasDatabaseName("IX_ProductReviews_External_Unique");

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
