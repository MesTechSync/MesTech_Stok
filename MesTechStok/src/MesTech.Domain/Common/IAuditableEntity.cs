namespace MesTech.Domain.Common;

/// <summary>
/// Audit bilgisi taşıyan entity'ler için interface.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime UpdatedAt { get; set; }
    string UpdatedBy { get; set; }
}
