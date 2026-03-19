using MediatR;
using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;

public record GetSuppliersCrmQuery(
    Guid TenantId,
    bool? IsActive = null,
    bool? IsPreferred = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetSuppliersCrmResult>;

public class GetSuppliersCrmResult
{
    public IReadOnlyList<SupplierCrmDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
