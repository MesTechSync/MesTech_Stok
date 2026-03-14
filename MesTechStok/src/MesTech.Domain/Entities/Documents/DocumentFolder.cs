using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Documents;

public class DocumentFolder : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentFolderId { get; private set; }
    public int Position { get; private set; }
    public bool IsSystem { get; private set; }

    private DocumentFolder() { }

    public static DocumentFolder Create(Guid tenantId, string name, int position = 0, Guid? parentFolderId = null, bool isSystem = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new DocumentFolder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Position = position,
            ParentFolderId = parentFolderId,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Rename(string newName)
    {
        if (IsSystem) throw new InvalidOperationException("System folders cannot be renamed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }
}
