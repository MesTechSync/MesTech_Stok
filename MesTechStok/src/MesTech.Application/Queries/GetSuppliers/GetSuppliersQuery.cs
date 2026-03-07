using MediatR;

namespace MesTech.Application.Queries.GetSuppliers;

public record GetSuppliersQuery(bool ActiveOnly = true) : IRequest<IReadOnlyList<SupplierListDto>>;

public class SupplierListDto
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
}
