using MediatR;

namespace MesTech.Application.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id) : IRequest<GetCustomerByIdResult?>;
