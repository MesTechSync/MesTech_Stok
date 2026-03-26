using MediatR;
using MesTech.Application.Commands.CreateSupplier;

namespace MesTech.Application.Commands.UpdateSupplier;

public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string Code,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    string? City = null,
    string? TaxNumber = null,
    string? TaxOffice = null,
    int PaymentTermDays = 0,
    bool IsActive = true
) : IRequest<SupplierCommandResult>;
