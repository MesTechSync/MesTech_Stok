using MediatR;
using MesTech.Application.Commands.CreateCustomer;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateCustomer;

public sealed class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, CustomerCommandResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CustomerCommandResult> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await _customerRepository.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (customer == null)
            return new CustomerCommandResult { IsSuccess = false, ErrorMessage = $"Customer {request.Id} not found." };

        customer.Name = request.Name;
        customer.Code = request.Code;
        customer.ContactPerson = request.ContactPerson;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.BillingAddress = request.BillingAddress;
        customer.ShippingAddress = request.ShippingAddress;
        customer.City = request.City;
        customer.TaxNumber = request.TaxNumber;
        customer.TaxOffice = request.TaxOffice;
        customer.PaymentTermDays = request.PaymentTermDays;
        customer.IsActive = request.IsActive;

        await _customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CustomerCommandResult
        {
            IsSuccess = true,
            CustomerId = customer.Id,
        };
    }
}
