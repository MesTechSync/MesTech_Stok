using FluentAssertions;
using MesTech.Application.Commands.CreateCariHareket;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateCariHareketHandlerTests
{
    private readonly Mock<ICariHareketRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly CreateCariHareketHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateCariHareketHandlerTests()
    {
        _repo = new Mock<ICariHareketRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new CreateCariHareketHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesHareketAndReturnsGuid()
    {
        var hesapId = Guid.NewGuid();
        var command = new CreateCariHareketCommand(
            _tenantId, hesapId, 1500m, CariDirection.Borc,
            "Satis tahsilati", DateTime.UtcNow, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.Is<CariHareket>(h =>
            h.CariHesapId == hesapId && h.Amount == 1500m &&
            h.Direction == CariDirection.Borc), It.IsAny<CancellationToken>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_WithInvoiceAndOrderRef_SetsReferences()
    {
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = new CreateCariHareketCommand(
            _tenantId, Guid.NewGuid(), 200m, CariDirection.Alacak,
            "Iade", null, invoiceId, orderId);

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<CariHareket>(h =>
            h.InvoiceId == invoiceId && h.OrderId == orderId), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullDate_DefaultsToUtcNow()
    {
        var command = new CreateCariHareketCommand(
            _tenantId, Guid.NewGuid(), 100m, CariDirection.Borc,
            "Test", null, null, null);

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<CariHareket>(h =>
            h.Date.Date == DateTime.UtcNow.Date), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
