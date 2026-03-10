using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetProductByBarcode;

public record GetProductByBarcodeQuery(string Barcode) : IRequest<ProductDto?>;
