using MediatR;

namespace MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;

/// <summary>
/// KVKK madde 11/c — kisisel verilerin disari aktarilmasi.
/// Tenant'in tum kisisel verisini JSON olarak dondurur.
/// </summary>
public record ExportPersonalDataQuery(Guid TenantId, Guid RequestedByUserId)
    : IRequest<PersonalDataExportDto>;

public record PersonalDataExportDto
{
    public Guid TenantId { get; init; }
    public DateTime ExportedAt { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public int UserCount { get; init; }
    public int StoreCount { get; init; }
    public int OrderCount { get; init; }
    public int ProductCount { get; init; }
    public string DataJson { get; init; } = "{}";
}
