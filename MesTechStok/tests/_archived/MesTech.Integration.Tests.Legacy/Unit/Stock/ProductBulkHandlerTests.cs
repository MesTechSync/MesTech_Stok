using FluentAssertions;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Application.Commands.CreateBulkProducts;
using MesTech.Application.Features.Product.Commands.ExecuteBulkImport;
using MesTech.Application.Features.Product.Commands.ValidateBulkImport;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Product")]
[Trait("Group", "BulkHandler")]
public class ProductBulkHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public ProductBulkHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    [Fact] public async Task BulkUpdateProducts_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new BulkUpdateProductsHandler(r.Object, _uow.Object, Mock.Of<ILogger<BulkUpdateProductsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ExecuteBulkImport_Null_Throws() { var s = new Mock<IBulkProductImportService>(); var h = new ExecuteBulkImportHandler(s.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ValidateBulkImport_Null_Throws() { var s = new Mock<IBulkProductImportService>(); var h = new ValidateBulkImportHandler(s.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
