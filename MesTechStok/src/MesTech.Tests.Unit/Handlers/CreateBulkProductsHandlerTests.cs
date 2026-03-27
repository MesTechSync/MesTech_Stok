using FluentAssertions;
using MesTech.Application.Commands.CreateBulkProducts;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateBulkProductsHandlerTests
{
    [Fact]
    public async Task Handle_CreatesProducts()
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        var sut = new CreateBulkProductsHandler(
            Mock.Of<IProductRepository>(), Mock.Of<IUnitOfWork>(), tenantProvider.Object);

        var cmd = new CreateBulkProductsCommand(5);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
    }
}
