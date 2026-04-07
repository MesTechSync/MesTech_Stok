using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Veritabani yedekleme gecmisi kaydi.
/// G383: GetBackupHistory stub→real data.
/// </summary>
public sealed class BackupEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FileName { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Status { get; private set; } = "Pending";
    public string? ErrorMessage { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private BackupEntry() { }

    public static BackupEntry Create(Guid tenantId, string fileName)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return new BackupEntry
        {
            TenantId = tenantId,
            FileName = fileName,
            Status = "InProgress"
        };
    }

    public void MarkCompleted(long sizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);
        SizeBytes = sizeBytes;
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}
