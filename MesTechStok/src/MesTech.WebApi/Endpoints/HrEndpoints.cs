using MediatR;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Application.Features.Hr.Commands.CreateTimeEntry;
using MesTech.Application.Features.Hr.Queries.GetDepartments;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using MesTech.Application.Features.Hr.Queries.GetLeaveRequests;
using MesTech.Application.Features.Hr.Queries.GetTimeEntries;
using MesTech.Domain.Enums;
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
        .Produces<IReadOnlyList<EmployeeDto>>(200)
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
        .WithSummary("İzin talebini onayla").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/hr/departments — departman listesi
        group.MapGet("/departments", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDepartmentsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDepartments")
        .WithSummary("Departman listesi")
        .Produces<IReadOnlyList<DepartmentDto>>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/hr/leaves — izin talepleri listesi (G207-DEV6)
        group.MapGet("/leaves", async (
            Guid tenantId,
            LeaveStatus? status,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetLeaveRequestsQuery(tenantId, status), ct);
            return Results.Ok(result);
        })
        .WithName("GetLeaveRequests")
        .WithSummary("İzin talepleri listesi — durum filtreli (G207)")
        .Produces<IReadOnlyList<LeaveRequestDto>>(200).Produces(400)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/hr/time-entries — zaman takip kayıtları
        group.MapGet("/time-entries", async (
            Guid tenantId, DateTime from, DateTime to,
            Guid? userId, int? page, int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetTimeEntriesQuery(tenantId, from, to, userId,
                    Math.Max(1, page ?? 1),
                    Math.Clamp(pageSize ?? 50, 1, 200)), ct);
            return Results.Ok(result);
        })
        .WithName("GetTimeEntries")
        .WithSummary("Zaman takip kayıtları — tarih aralığı ve kullanıcı filtreli")
        .Produces<IReadOnlyList<TimeEntryDto>>(200)
        .CacheOutput("Dashboard30s");

        // POST /api/v1/hr/time-entries — yeni zaman kaydı oluştur
        group.MapPost("/time-entries", async (
            CreateTimeEntryCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/hr/time-entries/{id}", new { id });
        })
        .WithName("CreateTimeEntry")
        .WithSummary("Yeni zaman takip kaydı — görev, süre, faturalanabilir")
        .Produces(201)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
