using MediatR;
using MesTech.Application.Features.Crm.Queries.GetCrmActivities;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// CRM aktivite geçmişi endpoint — müşteri/tedarikçi etkileşim kaydı.
/// DEV6 TUR13: Ayrı dosya (linter workaround).
/// </summary>
public static class CrmActivitiesEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/crm/activities")
            .WithTags("CRM")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/crm/activities — aktivite listesi (sayfalı)
        group.MapGet("/", async (
            Guid tenantId,
            Guid? contactId,
            int? page,
            int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCrmActivitiesQuery(tenantId, contactId, page ?? 1, Math.Clamp(pageSize ?? 50, 1, 200)), ct);
            return Results.Ok(result);
        })
        .WithName("GetCrmActivities")
        .WithSummary("CRM aktivite geçmişi — müşteri/tedarikçi etkileşimleri")
        .Produces(200);
    }
}
