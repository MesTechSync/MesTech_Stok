using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Queries.GetSupplierById;

public record GetSupplierByIdQuery(Guid SupplierId) : IRequest<Supplier?>;
