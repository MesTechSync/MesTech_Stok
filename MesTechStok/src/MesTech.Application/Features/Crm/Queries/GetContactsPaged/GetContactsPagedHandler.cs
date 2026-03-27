using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetContactsPaged;

public sealed class GetContactsPagedHandler : IRequestHandler<GetContactsPagedQuery, ContactsPagedResult>
{
    private readonly ICrmContactRepository _contactRepo;

    public GetContactsPagedHandler(ICrmContactRepository contactRepo)
        => _contactRepo = contactRepo ?? throw new ArgumentNullException(nameof(contactRepo));

    public async Task<ContactsPagedResult> Handle(GetContactsPagedQuery request, CancellationToken cancellationToken)
    {
        var all = await _contactRepo.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var filtered = string.IsNullOrWhiteSpace(request.Search)
            ? all
            : all.Where(c => c.FullName.Contains(request.Search, StringComparison.OrdinalIgnoreCase)).ToList();

        var paged = filtered
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ContactListDto
            {
                ContactId = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                Company = c.Company,
                CreatedAt = c.CreatedAt
            }).ToList();

        return new ContactsPagedResult
        {
            Contacts = paged,
            TotalCount = filtered.Count,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
