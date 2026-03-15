using MediatR;
using MesTech.Application.DTOs.Crm;
namespace MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
public record GetPipelineKanbanQuery(Guid TenantId, Guid PipelineId) : IRequest<KanbanBoardDto>;
