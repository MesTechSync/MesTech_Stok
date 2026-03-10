using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

public interface IProductScraperService
{
    Task<ScrapedProductDto?> ScrapeFromUrlAsync(string url, CancellationToken ct = default);
}
