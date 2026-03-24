using FluentAssertions;
using MesTech.Application.Commands.CreateCariHesap;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateCariHesapHandlerTests
{
    private readonly Mock<ICariHesapRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly CreateCariHesapHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateCariHesapHandlerTests()
    {
        _repo = new Mock<ICariHesapRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new CreateCariHesapHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesHesapAndReturnsGuid()
    {
        var command = new CreateCariHesapCommand(
            _tenantId, "ABC Ltd.", "1234567890", CariHesapType.Musteri,
            "0212-555-1234", "abc@test.com", "Istanbul");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.Is<CariHesap>(c =>
            c.Name == "ABC Ltd." && c.TaxNumber == "1234567890" &&
            c.Type == CariHesapType.Musteri)), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_SupplierType_CreatesCorrectType()
    {
        var command = new CreateCariHesapCommand(
            _tenantId, "Tedarikci", null, CariHesapType.Tedarikci,
            null, null, null);

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<CariHesap>(c =>
            c.Type == CariHesapType.Tedarikci)), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
