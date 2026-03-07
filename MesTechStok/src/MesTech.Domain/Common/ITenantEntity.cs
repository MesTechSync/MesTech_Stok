namespace MesTech.Domain.Common;

/// <summary>
/// Multi-tenant entity'ler için zorunlu interface.
/// Global Query Filter ile otomatik filtrelenir.
/// </summary>
public interface ITenantEntity
{
    int TenantId { get; set; }
}
