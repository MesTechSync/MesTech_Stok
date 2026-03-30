using MediatR;

namespace MesTech.Application.Features.Health.Queries.GetMesaStatus;

/// <summary>
/// MESA OS Bridge connection status — Ekran MESA Durum.
/// </summary>
public record GetMesaStatusQuery() : IRequest<MesaStatusDto>;
