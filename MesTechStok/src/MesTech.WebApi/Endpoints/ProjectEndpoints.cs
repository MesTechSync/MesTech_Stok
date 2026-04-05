using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Application.Features.Tasks.Commands.CompleteTask;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

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
        .WithSummary("Proje listesi (tenant bazlı)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/projects — yeni proje oluştur
        group.MapPost("/", async (
            CreateProjectCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/projects/{id}", new CreatedResponse(id));
        })
        .WithName("CreateProject")
        .WithSummary("Yeni proje oluştur").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/projects/{id}/tasks — projeye ait görev listesi
        group.MapGet("/{id:guid}/tasks", async (
            Guid id, WorkTaskStatus? status, Guid? assignedTo,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetProjectTasksQuery(id, status, assignedTo), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetProjectTasks")
        .WithSummary("Projeye ait görev listesi (durum + atanan filtresi)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/projects/tasks — yeni görev oluştur
        group.MapPost("/tasks", async (
            CreateWorkTaskCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/projects/tasks/{id}", new CreatedResponse(id));
        })
        .WithName("CreateWorkTask")
        .WithSummary("Yeni iş görevi oluştur").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/projects/tasks/{taskId}/complete — görevi tamamla
        group.MapPost("/tasks/{taskId:guid}/complete", async (
            Guid taskId, Guid userId,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new CompleteTaskCommand(taskId, userId), ct);
            return Results.NoContent();
        })
        .WithName("CompleteTask")
        .WithSummary("Görevi tamamlandı olarak işaretle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
