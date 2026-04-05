using MediatR;

namespace MesTech.Application.Commands.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id) : IRequest<DeleteCustomerResult>;
