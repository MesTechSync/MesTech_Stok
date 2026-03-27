using MediatR;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.AI.Commands.GenerateProductDescription;

public sealed class GenerateProductDescriptionHandler
    : IRequestHandler<GenerateProductDescriptionCommand, ProductDescriptionResult>
{
    private readonly IMesaAIService _mesaAI;
    private readonly ILogger<GenerateProductDescriptionHandler> _logger;

    public GenerateProductDescriptionHandler(IMesaAIService mesaAI, ILogger<GenerateProductDescriptionHandler> logger)
    {
        _mesaAI = mesaAI;
        _logger = logger;
    }

    public async Task<ProductDescriptionResult> Handle(
        GenerateProductDescriptionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AI urun aciklama uretimi: ProductId={ProductId}, Name={Name}",
            request.ProductId, request.ProductName);

        var result = await _mesaAI.GenerateProductDescriptionAsync(
            sku: request.ProductId.ToString("N")[..8],
            productName: request.ProductName,
            category: request.Category,
            imageUrls: null,
            ct: cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning("AI aciklama basarisiz: {Error}", result.ErrorMessage);
            return new ProductDescriptionResult { ErrorMessage = result.ErrorMessage };
        }

        var content = result.Content ?? string.Empty;
        var seoTitle = result.Metadata?.GetValueOrDefault("seo_title")
            ?? $"{request.ProductName} | {request.Brand ?? "MesTech"}";

        return new ProductDescriptionResult
        {
            IsSuccess = true,
            ShortDescription = content.Length > 100 ? content[..100] : content,
            MediumDescription = content.Length > 300 ? content[..300] : content,
            LongDescription = content,
            SeoKeywords = result.Metadata?.GetValueOrDefault("keywords")?.Split(',', StringSplitOptions.TrimEntries) ?? [],
            SeoTitle = seoTitle
        };
    }
}
