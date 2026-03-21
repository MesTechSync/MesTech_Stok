using MediatR;

namespace MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;

/// <summary>
/// KVKK madde 7 — kisisel verilerin silinmesi / anonimlestirilmesi.
/// Tenant'in tum kisisel verisini anonimlestirir (silme degil, anonim yap).
/// </summary>
public record DeletePersonalDataCommand(
    Guid TenantId, Guid RequestedByUserId, string Reason
) : IRequest<DeletePersonalDataResult>;

public record DeletePersonalDataResult(
    bool Success,
    int AnonymizedRecords,
    DateTime ProcessedAt);
