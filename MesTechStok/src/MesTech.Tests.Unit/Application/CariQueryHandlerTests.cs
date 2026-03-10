using FluentAssertions;
using MesTech.Application.Queries.GetCariHareketler;
using MesTech.Application.Queries.GetCariHesaplar;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class CariQueryHandlerTests
{
    private readonly Mock<ICariHesapRepository> _cariHesapRepo = new();
    private readonly Mock<ICariHareketRepository> _cariHareketRepo = new();

    private GetCariHesaplarHandler HesapHandler() => new(_cariHesapRepo.Object);
    private GetCariHareketlerHandler HareketHandler() => new(_cariHareketRepo.Object);

    private static CariHesap MakeHesap(Guid tenantId, CariHesapType type = CariHesapType.Musteri)
        => new() { TenantId = tenantId, Name = "Test Müşteri", Type = type };

    private static CariHareket MakeHareket(Guid hesapId, decimal amount, CariDirection dir = CariDirection.Borc)
        => new() { CariHesapId = hesapId, Amount = amount, Direction = dir,
                   TenantId = Guid.NewGuid(), Description = "test",
                   Date = DateTime.UtcNow };

    // ── GetCariHesaplar ──

    [Fact]
    public async Task GetCariHesaplar_NoFilter_ReturnsAll()
    {
        var tenantId = Guid.NewGuid();
        _cariHesapRepo.Setup(r => r.GetAllAsync(tenantId))
            .ReturnsAsync(new List<CariHesap>
            {
                MakeHesap(tenantId, CariHesapType.Musteri),
                MakeHesap(tenantId, CariHesapType.Tedarikci)
            });

        var result = await HesapHandler().Handle(
            new GetCariHesaplarQuery(TenantId: tenantId), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCariHesaplar_FilterByType_CallsGetByType()
    {
        var tenantId = Guid.NewGuid();
        _cariHesapRepo.Setup(r => r.GetByTypeAsync(CariHesapType.Tedarikci, tenantId))
            .ReturnsAsync(new List<CariHesap>
            {
                MakeHesap(tenantId, CariHesapType.Tedarikci)
            });

        var result = await HesapHandler().Handle(
            new GetCariHesaplarQuery(Type: CariHesapType.Tedarikci, TenantId: tenantId),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(CariHesapType.Tedarikci);
        _cariHesapRepo.Verify(r => r.GetByTypeAsync(CariHesapType.Tedarikci, tenantId), Times.Once);
        _cariHesapRepo.Verify(r => r.GetAllAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task GetCariHesaplar_NoResults_ReturnsEmptyList()
    {
        _cariHesapRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(new List<CariHesap>());

        var result = await HesapHandler().Handle(
            new GetCariHesaplarQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── GetCariHareketler ──

    [Fact]
    public async Task GetCariHareketler_NoDateFilter_CallsGetByCariHesapId()
    {
        var hesapId = Guid.NewGuid();
        _cariHareketRepo.Setup(r => r.GetByCariHesapIdAsync(hesapId))
            .ReturnsAsync(new List<CariHareket>
            {
                MakeHareket(hesapId, 500m, CariDirection.Borc),
                MakeHareket(hesapId, 300m, CariDirection.Alacak)
            });

        var result = await HareketHandler().Handle(
            new GetCariHareketlerQuery(CariHesapId: hesapId), CancellationToken.None);

        result.Should().HaveCount(2);
        _cariHareketRepo.Verify(r => r.GetByCariHesapIdAsync(hesapId), Times.Once);
    }

    [Fact]
    public async Task GetCariHareketler_WithDateRange_CallsGetByDateRange()
    {
        var hesapId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        _cariHareketRepo.Setup(r => r.GetByDateRangeAsync(hesapId, from, to))
            .ReturnsAsync(new List<CariHareket> { MakeHareket(hesapId, 1000m) });

        var result = await HareketHandler().Handle(
            new GetCariHareketlerQuery(CariHesapId: hesapId, From: from, To: to),
            CancellationToken.None);

        result.Should().HaveCount(1);
        _cariHareketRepo.Verify(r => r.GetByDateRangeAsync(hesapId, from, to), Times.Once);
        _cariHareketRepo.Verify(r => r.GetByCariHesapIdAsync(It.IsAny<Guid>()), Times.Never);
    }
}
