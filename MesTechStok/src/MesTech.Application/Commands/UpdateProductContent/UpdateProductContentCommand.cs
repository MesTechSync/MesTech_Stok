using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.UpdateProductContent;

public record UpdateProductContentCommand : IRequest
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string GeneratedContent { get; init; } = string.Empty;
    public string AiProvider { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
}

public sealed class UpdateProductContentHandler : IRequestHandler<UpdateProductContentCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductContentHandler> _logger;

    public UpdateProductContentHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, ILogger<UpdateProductContentHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(UpdateProductContentCommand request, CancellationToken cancellationToken)
    {
        // Try by ProductId first, fallback to SKU
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
                      ?? await _productRepository.GetBySKUAsync(request.SKU, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            _logger.LogWarning("UpdateProductContent: Product not found — ProductId={ProductId}, SKU={SKU}", request.ProductId, request.SKU);
            return;
        }

        product.Description = request.GeneratedContent;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = "mesa-ai";
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UpdateProductContent: Product {SKU} content updated by {AiProvider}", request.SKU, request.AiProvider);
    }
}
