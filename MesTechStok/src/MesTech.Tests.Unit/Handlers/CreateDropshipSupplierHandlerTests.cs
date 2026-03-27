using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateDropshipSupplierHandlerTests
{
    private readonly Mock<IDropshipSupplierRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateDropshipSupplierHandler _sut;

    public CreateDropshipSupplierHandlerTests()
    {
        _sut = new CreateDropshipSupplierHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuid()
    {
        var cmd = new CreateDropshipSupplierCommand(
            Guid.NewGuid(), "Test Supplier", "https://supplier.com", DropshipMarkupType.Percentage);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<DropshipSupplier>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
