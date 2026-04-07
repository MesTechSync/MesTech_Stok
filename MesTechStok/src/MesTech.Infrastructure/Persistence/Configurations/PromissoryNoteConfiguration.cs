using MesTech.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PromissoryNoteConfiguration : IEntityTypeConfiguration<PromissoryNote>
{
    public void Configure(EntityTypeBuilder<PromissoryNote> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.NoteNumber).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Amount).HasPrecision(18, 2);
        builder.Property(n => n.DebtorName).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Notes).HasMaxLength(2000);

        builder.HasIndex(n => n.TenantId).HasDatabaseName("IX_PromissoryNotes_TenantId");
        builder.HasIndex(n => new { n.TenantId, n.NoteNumber }).IsUnique()
            .HasDatabaseName("UX_PromissoryNotes_Tenant_Number");
    }
}
