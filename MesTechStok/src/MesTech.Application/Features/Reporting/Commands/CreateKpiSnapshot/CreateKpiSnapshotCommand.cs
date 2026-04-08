using MediatR;
using MesTech.Domain.Entities.Reporting;

namespace MesTech.Application.Features.Reporting.Commands.CreateKpiSnapshot;

public record CreateKpiSnapshotCommand(
    Guid TenantId,
    DateTime SnapshotDate,
    KpiType Type,
    decimal Value,
    decimal? PreviousValue = null,
    string? PlatformCode = null
) : IRequest<Guid>;
