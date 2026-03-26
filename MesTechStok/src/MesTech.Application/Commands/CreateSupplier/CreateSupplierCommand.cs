using MediatR;

namespace MesTech.Application.Commands.CreateSupplier;

public record CreateSupplierCommand(
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

public sealed class SupplierCommandResult
{
    public bool IsSuccess { get; set; }
    public Guid SupplierId { get; set; }
    public string? ErrorMessage { get; set; }
}
