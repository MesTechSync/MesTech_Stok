using MediatR;

namespace MesTech.Application.Queries.GetSuppliersPaged;

public record GetSuppliersPagedQuery(
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedSupplierResult>;

public sealed class PagedSupplierResult
{
    public IReadOnlyList<SupplierItemDto> Items { get; set; } = Array.Empty<SupplierItemDto>();
    public int TotalCount { get; set; }
}

public sealed class SupplierItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public bool IsPreferred { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime CreatedDate { get; set; }
}
