using MediatR;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;

public record ImportFromFeedCommand(
    Guid FeedSourceId,
    List<string> SelectedSkus,
    decimal PriceMultiplier
) : IRequest<ImportResultDto>;
