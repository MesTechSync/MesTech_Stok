using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetInventoryStatistics;

public record GetInventoryStatisticsQuery : IRequest<InventoryStatisticsDto>;
