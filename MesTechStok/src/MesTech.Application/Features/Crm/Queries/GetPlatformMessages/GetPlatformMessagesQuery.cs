using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Crm.Queries.GetPlatformMessages;

public record GetPlatformMessagesQuery(
    Guid TenantId,
    PlatformType? Platform = null,
    MessageStatus? Status = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<GetPlatformMessagesResult>;

public class GetPlatformMessagesResult
{
    public IReadOnlyList<PlatformMessageDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
