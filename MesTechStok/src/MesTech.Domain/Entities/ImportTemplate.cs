using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Icerik aktarma sablonu — CSV/XML/Excel/API import sablonlari.
/// Kolon eslestirme ve format bilgisi saklanir.
/// </summary>
public sealed class ImportTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public int FieldCount { get; set; }
    public DateTime? LastUsedAt { get; set; }

    private readonly List<ImportFieldMapping> _mappings = new();
    public IReadOnlyList<ImportFieldMapping> Mappings => _mappings.AsReadOnly();

    private ImportTemplate() { }

    public static ImportTemplate Create(Guid tenantId, string name, string format)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        return new ImportTemplate
        {
            TenantId = tenantId,
            Name = name,
            Format = format
        };
    }

    public void AddMapping(string sourceColumn, string targetField)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceColumn);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetField);

        _mappings.Add(ImportFieldMapping.Create(Id, sourceColumn, targetField));
        FieldCount = _mappings.Count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Kolon eslestirme — kaynak sutun adi ile hedef alan adi.
/// </summary>
public sealed class ImportFieldMapping : BaseEntity
{
    public Guid ImportTemplateId { get; set; }
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;

    public ImportTemplate ImportTemplate { get; set; } = null!;

    private ImportFieldMapping() { }

    internal static ImportFieldMapping Create(Guid templateId, string sourceColumn, string targetField)
    {
        return new ImportFieldMapping
        {
            ImportTemplateId = templateId,
            SourceColumn = sourceColumn,
            TargetField = targetField
        };
    }
}
