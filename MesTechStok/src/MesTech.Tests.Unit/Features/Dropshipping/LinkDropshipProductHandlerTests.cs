using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Dropshipping;

[Trait("Category", "Unit")]
public class LinkDropshipProductHandlerTests
{
    private readonly Mock<IDropshipProductRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly LinkDropshipProductHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public LinkDropshipProductHandlerTests()
        => _sut = new LinkDropshipProductHandler(_repoMock.Object, _uowMock.Object);

    [Fact]
    public async Task Handle_ValidCommand_LinksAndPersists()
    {
        var dropshipId = Guid.NewGuid();
        var mesTechProductId = Guid.NewGuid();
        var product = DropshipProduct.Create(_tenantId, Guid.NewGuid(), "DS-SKU-001", "Test Product", 100m, 50);

        _repoMock.Setup(r => r.GetByIdAsync(dropshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var cmd = new LinkDropshipProductCommand(_tenantId, dropshipId, mesTechProductId);
        await _sut.Handle(cmd, CancellationToken.None);

        _repoMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once());
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsInvalidOperationException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshipProduct?)null);

        var cmd = new LinkDropshipProductCommand(_tenantId, Guid.NewGuid(), Guid.NewGuid());
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WrongTenant_ThrowsInvalidOperationException()
    {
        var differentTenant = Guid.NewGuid();
        var product = DropshipProduct.Create(differentTenant, Guid.NewGuid(), "DS-SKU-002", "Other Tenant", 50m, 10);
        var dropshipId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByIdAsync(dropshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var cmd = new LinkDropshipProductCommand(_tenantId, dropshipId, Guid.NewGuid());
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tenant*");
    }
}
