using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetCustomersPaged;

public sealed class GetCustomersPagedHandler : IRequestHandler<GetCustomersPagedQuery, PagedCustomerResult>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersPagedHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
    }

    public async Task<PagedCustomerResult> Handle(GetCustomersPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allCustomers = await _customerRepository.GetAllAsync().ConfigureAwait(false);

        var filtered = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? allCustomers
            : allCustomers.Where(c =>
                c.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                CustomerType = c.CustomerType,
                Email = c.Email,
                Phone = c.Phone,
                City = c.City,
                IsActive = c.IsActive,
                CurrentBalance = c.CurrentBalance,
                CreatedDate = c.CreatedAt,
            })
            .ToList();

        return new PagedCustomerResult
        {
            Items = items,
            TotalCount = totalCount,
        };
    }
}
