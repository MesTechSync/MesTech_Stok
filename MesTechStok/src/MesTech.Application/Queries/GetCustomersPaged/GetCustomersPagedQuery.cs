using MediatR;

namespace MesTech.Application.Queries.GetCustomersPaged;

public record GetCustomersPagedQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedCustomerResult>;

public sealed class PagedCustomerResult
{
    public IReadOnlyList<CustomerItemDto> Items { get; set; } = Array.Empty<CustomerItemDto>();
    public int TotalCount { get; set; }
}

public sealed class CustomerItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime CreatedDate { get; set; }
}
