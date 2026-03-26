using MediatR;

namespace MesTech.Application.Commands.CreateCustomer;

public record CreateCustomerCommand(
    string Name,
    string Code,
    string CustomerType = "INDIVIDUAL",
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

public sealed class CustomerCommandResult
{
    public bool IsSuccess { get; set; }
    public Guid CustomerId { get; set; }
    public string? ErrorMessage { get; set; }
}
