using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetPlatformMessages;

public sealed class GetPlatformMessagesHandler : IRequestHandler<GetPlatformMessagesQuery, GetPlatformMessagesResult>
{
    private readonly IPlatformMessageRepository _repository;

    public GetPlatformMessagesHandler(IPlatformMessageRepository repository)
        => _repository = repository;

    public async Task<GetPlatformMessagesResult> Handle(GetPlatformMessagesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.TenantId, request.Platform, request.Status,
            request.Page, request.PageSize, cancellationToken);

        return new GetPlatformMessagesResult
        {
            Items = items.Select(m => new PlatformMessageDto
            {
                Id = m.Id,
                Platform = m.Platform.ToString(),
                SenderName = m.SenderName,
                Subject = m.Subject,
                BodyPreview = m.Body.Length > 120 ? m.Body[..120] + "..." : m.Body,
                Status = m.Status.ToString(),
                Direction = m.Direction.ToString(),
                HasAiSuggestion = m.AiSuggestedReply is not null,
                AiSuggestedReply = m.AiSuggestedReply,
                CustomerId = m.CustomerId,
                OrderId = m.OrderId,
                ReceivedAt = m.ReceivedAt,
                RepliedAt = m.RepliedAt,
                RepliedBy = m.RepliedBy
            }).ToList().AsReadOnly(),
            TotalCount = totalCount
        };
    }
}
