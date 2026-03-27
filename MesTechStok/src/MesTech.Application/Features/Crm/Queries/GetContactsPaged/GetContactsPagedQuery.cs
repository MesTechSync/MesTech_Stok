using MediatR;

namespace MesTech.Application.Features.Crm.Queries.GetContactsPaged;

public record GetContactsPagedQuery(Guid TenantId, int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<ContactsPagedResult>;

public sealed class ContactsPagedResult
{
    public IReadOnlyList<ContactListDto> Contacts { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class ContactListDto
{
    public Guid ContactId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Company { get; init; }
    public DateTime CreatedAt { get; init; }
}
