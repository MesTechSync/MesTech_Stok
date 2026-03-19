using MediatR;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;

public record PreviewFeedCommand(Guid FeedSourceId) : IRequest<FeedPreviewDto>;
