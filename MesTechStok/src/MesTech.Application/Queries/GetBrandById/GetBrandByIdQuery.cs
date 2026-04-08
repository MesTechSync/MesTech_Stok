using MediatR;

namespace MesTech.Application.Queries.GetBrandById;

public record GetBrandByIdQuery(Guid Id) : IRequest<GetBrandByIdResult?>;
