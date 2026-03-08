using MediatR;

namespace MesTech.Application.Queries.GetDashboardData;

public record GetActiveCategoriesCountQuery() : IRequest<int>;
