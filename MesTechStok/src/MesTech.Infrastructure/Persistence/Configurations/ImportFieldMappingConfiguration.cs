using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ImportFieldMappingConfiguration : IEntityTypeConfiguration<ImportFieldMapping>
{
    public void Configure(EntityTypeBuilder<ImportFieldMapping> builder)
    {
        builder.Property(x => x.SourceColumn).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetField).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.ImportTemplateId);
    }
}
