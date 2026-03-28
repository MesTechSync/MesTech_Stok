using FluentAssertions;
using MesTech.Application.Commands.DeleteProduct;
using MesTech.Application.Commands.UpdateProduct;
using MesTech.Application.Features.Product.Queries.GetProductById;
using MesTech.Application.Features.Product.Queries.GetProductByBarcode;
using MesTech.Application.Features.Product.Commands.MapProductToPlatform;
using MesTech.Application.Features.Product.Commands.FetchProductFromUrl;
using MesTech.Application.Features.Product.Commands.GenerateProductDescription;
using MesTech.Application.Features.Product.Queries.SearchProductsForImageMatch;
using MesTech.Application.Features.Product.Commands.UpdateProductContent;
using MesTech.Application.Features.Product.Commands.UpdateProductPrice;
using MesTech.Application.Features.Product.Commands.UpdateProductImage;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Product")]
[Trait("Group", "Handler")]
public class ProductExtendedHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public ProductExtendedHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    [Fact] public async Task DeleteProduct_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new DeleteProductHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateProduct_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new UpdateProductHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetProductById_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new GetProductByIdHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetProductByBarcode_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new GetProductByBarcodeHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task MapProductToPlatform_Null_Throws() { var r = new Mock<IProductRepository>(); var t = new Mock<ITenantProvider>(); var h = new MapProductToPlatformHandler(r.Object, _uow.Object, t.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task FetchProductFromUrl_Null_Throws() { var h = new FetchProductFromUrlHandler(Mock.Of<ILogger<FetchProductFromUrlHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GenerateProductDescription_Null_Throws() { var ai = new Mock<IMesaAIService>(); var h = new GenerateProductDescriptionHandler(ai.Object, Mock.Of<ILogger<GenerateProductDescriptionHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task SearchProductsForImageMatch_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new SearchProductsForImageMatchHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateProductContent_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new UpdateProductContentHandler(r.Object, _uow.Object, Mock.Of<ILogger<UpdateProductContentHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateProductPrice_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new UpdateProductPriceHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task UpdateProductImage_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new UpdateProductImageHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ExportProducts_Null_Throws() { var s = new Mock<IBulkProductImportService>(); var h = new ExportProductsHandler(s.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
