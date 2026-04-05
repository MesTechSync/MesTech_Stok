namespace MesTech.Application.Commands.DeleteCustomer;

public sealed record DeleteCustomerResult(bool IsSuccess, string? ErrorMessage = null);
