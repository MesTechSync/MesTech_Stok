using MediatR;

namespace MesTech.Application.Features.AI.Commands.GenerateProductDescription;

public record GenerateProductDescriptionCommand(
    Guid ProductId,
    Guid TenantId,
    string ProductName,
    string? Category,
    string? Brand,
    IReadOnlyList<string>? Features,
    string Language = "tr") : IRequest<ProductDescriptionResult>;

public sealed class ProductDescriptionResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string ShortDescription { get; init; } = string.Empty;
    public string MediumDescription { get; init; } = string.Empty;
    public string LongDescription { get; init; } = string.Empty;
    public IReadOnlyList<string> SeoKeywords { get; init; } = [];
    public string? SeoTitle { get; init; }
}
