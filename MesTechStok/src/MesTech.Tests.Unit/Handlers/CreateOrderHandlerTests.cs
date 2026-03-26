using FluentAssertions;
using MesTech.Application.Commands.CreateOrder;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateOrderHandler _sut;

    public CreateOrderHandlerTests()
    {
        _sut = new CreateOrderHandler(_orderRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderAndReturnsSuccess()
    {
        var cmd = new CreateOrderCommand(Guid.NewGuid(), "Test Müşteri", "test@test.com", "MANUAL", "Not");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.OrderId.Should().NotBe(Guid.Empty);
        result.OrderNumber.Should().StartWith("ORD-");

        _orderRepoMock.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.CustomerName == "Test Müşteri" &&
            o.Type == "MANUAL")), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NullOptionalFields_CreatesOrder()
    {
        var cmd = new CreateOrderCommand(Guid.NewGuid(), "Müşteri", null, "IMPORT", null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _orderRepoMock.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.CustomerEmail == null && o.Notes == null)), Times.Once);
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new CreateOrderHandler(null!, _uowMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        var act = () => new CreateOrderHandler(_orderRepoMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
