using MediatR;
using MesTech.Application.Commands.CreateCustomer;

namespace MesTech.Application.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Code,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? BillingAddress = null,
    string? ShippingAddress = null,
    string? City = null,
    string? TaxNumber = null,
    string? TaxOffice = null,
    int PaymentTermDays = 0,
    bool IsActive = true
) : IRequest<CustomerCommandResult>;
