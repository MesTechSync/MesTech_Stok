using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateAutoOrderHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IDropshipSupplierRepository> _supplierRepoMock = new();
    private readonly Mock<IDropshipOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateAutoOrderHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateAutoOrderHandlerTests()
    {
        _sut = new CreateAutoOrderHandler(
            _productRepoMock.Object, _supplierRepoMock.Object, _orderRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
