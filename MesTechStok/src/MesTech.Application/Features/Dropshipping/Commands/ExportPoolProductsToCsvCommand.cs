using FluentValidation;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands;

public record ExportPoolProductsToCsvCommand(
    Guid PoolId,
    IEnumerable<Guid> ProductIds,
    decimal PriceMarkupPercent,
    bool HideSupplierInfo
) : IRequest<byte[]>;

public sealed class ExportPoolProductsToCsvCommandValidator : AbstractValidator<ExportPoolProductsToCsvCommand>
{
    public ExportPoolProductsToCsvCommandValidator()
    {
        RuleFor(x => x.PoolId).NotEmpty();
        RuleFor(x => x.PriceMarkupPercent).GreaterThanOrEqualTo(0).LessThanOrEqualTo(500);
    }
}

public sealed class ExportPoolProductsToCsvCommandHandler(
    IDropshippingPoolRepository poolRepo,
    ICsvExportService csvExport
) : IRequestHandler<ExportPoolProductsToCsvCommand, byte[]>
{
    public async Task<byte[]> Handle(
        ExportPoolProductsToCsvCommand req, CancellationToken cancellationToken)
    {
        var poolProducts = await poolRepo.GetPoolProductsByIdsAsync(
            req.PoolId, req.ProductIds, cancellationToken);

        var exportDtos = poolProducts.Select(p => new ProductExportDto(
            Sku: p.Product?.SKU ?? string.Empty,
            Name: p.Product?.Name ?? string.Empty,
            Price: ApplyMarkup(p.PoolPrice, req.PriceMarkupPercent),
            Stock: p.Product?.Stock ?? 0,
            Category: null,
            Barcode: p.Product?.Barcode
        )).ToList();

        var stream = await csvExport.ExportProductsAsync(exportDtos, cancellationToken);

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    private static decimal ApplyMarkup(decimal price, decimal pct)
        => pct > 0 ? price * (1 + pct / 100) : price;
}
