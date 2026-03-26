using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateCustomer;

public sealed class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CustomerCommandResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CustomerCommandResult> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = new Customer
        {
            Name = request.Name,
            Code = request.Code,
            CustomerType = request.CustomerType,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            BillingAddress = request.BillingAddress,
            ShippingAddress = request.ShippingAddress,
            City = request.City,
            TaxNumber = request.TaxNumber,
            TaxOffice = request.TaxOffice,
            PaymentTermDays = request.PaymentTermDays,
            IsActive = request.IsActive,
        };

        await _customerRepository.AddAsync(customer).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CustomerCommandResult
        {
            IsSuccess = true,
            CustomerId = customer.Id,
        };
    }
}
