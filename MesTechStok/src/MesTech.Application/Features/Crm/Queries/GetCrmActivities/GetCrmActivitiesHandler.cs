using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Crm.Queries.GetCrmActivities;

public sealed class GetCrmActivitiesHandler : IRequestHandler<GetCrmActivitiesQuery, CrmActivitiesResult>
{
    private readonly IActivityRepository _activityRepo;
    private readonly ICrmContactRepository _contactRepo;
    private readonly ILogger<GetCrmActivitiesHandler> _logger;

    public GetCrmActivitiesHandler(
        IActivityRepository activityRepo,
        ICrmContactRepository contactRepo,
        ILogger<GetCrmActivitiesHandler> logger)
    {
        _activityRepo = activityRepo ?? throw new ArgumentNullException(nameof(activityRepo));
        _contactRepo = contactRepo ?? throw new ArgumentNullException(nameof(contactRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CrmActivitiesResult> Handle(GetCrmActivitiesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching CRM activities for tenant {TenantId}, contact {ContactId}",
            request.TenantId, request.ContactId);

        var activities = request.ContactId.HasValue
            ? await _activityRepo.GetByContactAsync(request.ContactId.Value, cancellationToken).ConfigureAwait(false)
            : await _activityRepo.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        // Build contact name lookup
        var contacts = await _contactRepo.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        var contactNames = contacts.ToDictionary(c => c.Id, c => c.FullName);

        var paged = activities
            .OrderByDescending(a => a.OccurredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new CrmActivityDto
            {
                Id = a.Id,
                Type = a.Type,
                Subject = a.Subject,
                Description = a.Description,
                ContactName = a.CrmContactId.HasValue && contactNames.TryGetValue(a.CrmContactId.Value, out var name)
                    ? name
                    : null,
                OccurredAt = a.OccurredAt,
                IsCompleted = a.IsCompleted,
                CreatedAt = a.CreatedAt
            }).ToList();

        return new CrmActivitiesResult
        {
            Activities = paged,
            TotalCount = activities.Count,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
