using MediatR;
using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Features.Crm.Queries.GetCustomersCrm;

public record GetCustomersCrmQuery(
    Guid TenantId,
    bool? IsVip = null,
    bool? IsActive = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetCustomersCrmResult>;

public sealed class GetCustomersCrmResult
{
    public IReadOnlyList<CustomerCrmDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
