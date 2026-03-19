using MediatR;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;

public record FetchProductFromUrlCommand(string ProductUrl) : IRequest<FetchedProductDto?>;
