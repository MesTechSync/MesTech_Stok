using MediatR;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class HrEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/hr")
            .WithTags("HR")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/hr/employees — employee list
        group.MapGet("/employees", async (
            ISender mediator,
            Guid tenantId,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetEmployeesQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetEmployees")
        .WithSummary("Çalışan listesi")
        .CacheOutput("Lookup60s");

        // POST /api/v1/hr/leaves/{leaveId}/approve — approve leave request
        group.MapPost("/leaves/{leaveId:guid}/approve", async (
            ISender mediator,
            Guid leaveId,
            Guid approverUserId,
            CancellationToken ct = default) =>
        {
            await mediator.Send(new ApproveLeaveCommand(leaveId, approverUserId), ct);
            return Results.NoContent();
        })
        .WithName("ApproveLeave")
        .WithSummary("İzin talebini onayla");
    }
}
