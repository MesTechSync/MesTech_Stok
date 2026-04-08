using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateCustomer;

public sealed class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CustomerCommandResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public CreateCustomerHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
    }

    public async Task<CustomerCommandResult> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = _tenantProvider.GetCurrentTenantId();

        var customer = Customer.Create(
            tenantId,
            request.Name,
            request.Code,
            request.Email,
            request.Phone);

        customer.CustomerType = request.CustomerType;
        customer.ContactPerson = request.ContactPerson;
        customer.BillingAddress = request.BillingAddress;
        customer.ShippingAddress = request.ShippingAddress;
        customer.City = request.City;
        customer.TaxNumber = request.TaxNumber;
        customer.TaxOffice = request.TaxOffice;
        customer.PaymentTermDays = request.PaymentTermDays;
        customer.IsActive = request.IsActive;

        await _customerRepository.AddAsync(customer, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CustomerCommandResult
        {
            IsSuccess = true,
            CustomerId = customer.Id,
        };
    }
}
