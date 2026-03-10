using FluentAssertions;
using MesTech.Application.Commands.CreateCariHareket;
using MesTech.Application.Commands.CreateCariHesap;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class CariCommandHandlerTests
{
    private readonly Mock<ICariHesapRepository> _cariHesapRepo = new();
    private readonly Mock<ICariHareketRepository> _cariHareketRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateCariHesapHandler HesapCreateHandler() =>
        new(_cariHesapRepo.Object, _unitOfWork.Object);

    private UpdateCariHesapHandler HesapUpdateHandler() =>
        new(_cariHesapRepo.Object, _unitOfWork.Object);

    private CreateCariHareketHandler HareketHandler() =>
        new(_cariHareketRepo.Object, _unitOfWork.Object);

    // ── CreateCariHesap ──

    [Fact]
    public async Task CreateCariHesap_ValidCommand_ReturnsNonEmptyGuid()
    {
        var tenantId = Guid.NewGuid();
        var command = new CreateCariHesapCommand(
            tenantId, "Acme Ltd", "1234567890",
            CariHesapType.Musteri, "+90 555 000 00 00",
            "acme@example.com", "İstanbul");

        var result = await HesapCreateHandler().Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _cariHesapRepo.Verify(r => r.AddAsync(It.Is<CariHesap>(h =>
            h.TenantId == tenantId &&
            h.Name == "Acme Ltd" &&
            h.Type == CariHesapType.Musteri)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCariHesap_Tedarikci_TypePersisted()
    {
        var command = new CreateCariHesapCommand(
            Guid.NewGuid(), "Tedarikçi A.Ş.", null,
            CariHesapType.Tedarikci, null, null, null);

        CariHesap? captured = null;
        _cariHesapRepo
            .Setup(r => r.AddAsync(It.IsAny<CariHesap>()))
            .Callback<CariHesap>(h => captured = h);

        await HesapCreateHandler().Handle(command, CancellationToken.None);

        captured!.Type.Should().Be(CariHesapType.Tedarikci);
        captured.TaxNumber.Should().BeNull();
    }

    // ── UpdateCariHesap ──

    [Fact]
    public async Task UpdateCariHesap_ExistingId_UpdatesFieldsAndReturnsTrue()
    {
        var existing = new CariHesap
        {
            TenantId = Guid.NewGuid(),
            Name = "Eski Ad",
            Type = CariHesapType.Musteri
        };
        _cariHesapRepo.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);

        var command = new UpdateCariHesapCommand(
            existing.Id, "Yeni Ad", "9876543210",
            CariHesapType.Tedarikci, null, null, null);

        var result = await HesapUpdateHandler().Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        existing.Name.Should().Be("Yeni Ad");
        existing.Type.Should().Be(CariHesapType.Tedarikci);
        existing.TaxNumber.Should().Be("9876543210");
        _cariHesapRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCariHesap_NotFound_ReturnsFalseAndSkipsSave()
    {
        _cariHesapRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CariHesap?)null);

        var command = new UpdateCariHesapCommand(
            Guid.NewGuid(), "Ad", null,
            CariHesapType.Musteri, null, null, null);

        var result = await HesapUpdateHandler().Handle(command, CancellationToken.None);

        result.Should().BeFalse();
        _cariHesapRepo.Verify(r => r.UpdateAsync(It.IsAny<CariHesap>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── CreateCariHareket ──

    [Fact]
    public async Task CreateCariHareket_BorcDirection_ReturnsNonEmptyGuid()
    {
        var tenantId = Guid.NewGuid();
        var hesapId = Guid.NewGuid();
        var command = new CreateCariHareketCommand(
            TenantId: tenantId,
            CariHesapId: hesapId,
            Amount: 750m,
            Direction: CariDirection.Borc,
            Description: "Fatura #001",
            Date: null,
            InvoiceId: null,
            OrderId: null);

        var result = await HareketHandler().Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _cariHareketRepo.Verify(r => r.AddAsync(It.Is<CariHareket>(h =>
            h.TenantId == tenantId &&
            h.CariHesapId == hesapId &&
            h.Amount == 750m &&
            h.Direction == CariDirection.Borc)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCariHareket_NullDate_UsesUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var command = new CreateCariHareketCommand(
            Guid.NewGuid(), Guid.NewGuid(),
            100m, CariDirection.Alacak,
            "desc", null, null, null);

        CariHareket? captured = null;
        _cariHareketRepo
            .Setup(r => r.AddAsync(It.IsAny<CariHareket>()))
            .Callback<CariHareket>(h => captured = h);

        await HareketHandler().Handle(command, CancellationToken.None);

        captured!.Date.Should().BeOnOrAfter(before);
        captured.Date.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
