namespace MesTech.Application.DTOs.Invoice;

public record ScrapedProductDto(
    string Title,
    decimal Price,
    string? ImageUrl,
    string? Barcode,
    string Platform,
    string? CategoryPath,
    string? Brand,
    string? Description);
