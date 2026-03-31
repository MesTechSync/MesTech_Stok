using MediatR;

namespace MesTech.Application.Commands.DeleteSupplier;

public record DeleteSupplierCommand(Guid SupplierId) : IRequest<bool>;
