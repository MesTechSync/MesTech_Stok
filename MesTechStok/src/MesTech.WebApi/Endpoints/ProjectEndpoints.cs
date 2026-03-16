using MediatR;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class ProjectEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/projects")
            .WithTags("Projects")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/projects — proje listesi
        group.MapGet("/", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetProjectsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetProjects")
        .WithSummary("Proje listesi (tenant bazlı)");

        // POST /api/v1/projects — yeni proje oluştur
        group.MapPost("/", async (
            CreateProjectCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/projects/{id}", new { id });
        })
        .WithName("CreateProject")
        .WithSummary("Yeni proje oluştur");

        // GET /api/v1/projects/{id}/tasks — projeye ait görev listesi
        group.MapGet("/{id:guid}/tasks", async (
            Guid id, WorkTaskStatus? status, Guid? assignedTo,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetProjectTasksQuery(id, status, assignedTo), ct);
            return Results.Ok(result);
        })
        .WithName("GetProjectTasks")
        .WithSummary("Projeye ait görev listesi (durum + atanan filtresi)");
    }
}
