using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;

/// <summary>
/// KDV beyanname taslak sorgulama — aylik KDV1/KDV2 draft hesaplama.
/// </summary>
/// <param name="TenantId">Kiracı ID.</param>
/// <param name="Period">Donem (yyyy-MM formati, ornegin "2026-03").</param>
public record GetKdvDeclarationDraftQuery(Guid TenantId, string Period)
    : IRequest<KdvDeclarationDraftDto>;
