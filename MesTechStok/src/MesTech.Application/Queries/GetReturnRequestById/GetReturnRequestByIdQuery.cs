using MediatR;

namespace MesTech.Application.Queries.GetReturnRequestById;

public record GetReturnRequestByIdQuery(Guid Id) : IRequest<GetReturnRequestByIdResult?>;
