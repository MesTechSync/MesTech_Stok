using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Queries.GetProductDbStatus;

/// <summary>
/// Urun veritabani durum sorgusu — toplam, aktif, stok tukenen urun sayilarini doner.
/// Dashboard ve ProductsView tarafindan kullanilir.
/// </summary>
public sealed class GetProductDbStatusHandler
    : IRequestHandler<GetProductDbStatusQuery, ProductDbStatusDto>
{
    private readonly IProductRepository _productRepo;
    private readonly ILogger<GetProductDbStatusHandler> _logger;

    public GetProductDbStatusHandler(
        IProductRepository productRepo,
        ILogger<GetProductDbStatusHandler> logger)
    {
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<ProductDbStatusDto> Handle(
        GetProductDbStatusQuery request, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031 // Catch general exception — return safe fallback
        try
        {
            var totalCount = await _productRepo.GetCountAsync(cancellationToken).ConfigureAwait(false);
            var allProducts = await _productRepo.GetAllAsync(cancellationToken).ConfigureAwait(false);

            var activeCount = allProducts.Count(p => p.IsActive);

            return new ProductDbStatusDto
            {
                IsConnected = true,
                TotalCount = totalCount,
                ActiveCount = activeCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Urun DB durum sorgusu hatasi");
            return new ProductDbStatusDto
            {
                IsConnected = false,
                TotalCount = 0,
                ActiveCount = 0
            };
        }
#pragma warning restore CA1031
    }
}
