using FluentAssertions;
using MesTech.Application.DTOs.Dropshipping;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using Moq;

namespace MesTech.Tests.Unit.Application.Orders;

[Trait("Category", "Unit")]
[Trait("Domain", "Orders")]
public class GetDropshipOrdersHandlerTests
{
    private readonly Mock<IDropshipOrderRepository> _repo = new();

    private GetDropshipOrdersHandler CreateSut() => new(_repo.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsMappedDropshipOrders()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetDropshipOrdersQuery(tenantId);

        var dropshipOrder = DropshipOrder.Create(tenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _repo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipOrder> { dropshipOrder }.AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be("Pending");
        result[0].OrderId.Should().Be(dropshipOrder.OrderId);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetDropshipOrdersQuery(tenantId);

        _repo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipOrder>().AsReadOnly());

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
