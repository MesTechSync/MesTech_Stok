using FluentAssertions;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5 Batch 8: Settings + Cari Hesap handler testleri.
/// </summary>

#region UpdateCariHesap

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateCariHesapHandlerTests
{
    private readonly Mock<ICariHesapRepository> _cariRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateCariHesapHandler CreateSut() => new(_cariRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_NotFound_ShouldReturnFalse()
    {
        _cariRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CariHesap?)null);
        var cmd = new UpdateCariHesapCommand(Guid.NewGuid(), "Test", null, CariHesapType.Musteri, null, null, null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateAndReturnTrue()
    {
        var cari = new CariHesap { Name = "Old", TenantId = Guid.NewGuid(), Type = CariHesapType.Musteri };
        _cariRepo.Setup(r => r.GetByIdAsync(cari.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cari);

        var cmd = new UpdateCariHesapCommand(
            cari.Id, "Yeni Müşteri", "1234567890", CariHesapType.Tedarikci,
            "05551234567", "test@test.com", "İstanbul");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        cari.Name.Should().Be("Yeni Müşteri");
        cari.Type.Should().Be(CariHesapType.Tedarikci);
        cari.Phone.Should().Be("05551234567");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region UpdateProfileSettings

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateProfileSettingsHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateProfileSettingsHandler CreateSut() => new(_tenantRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_TenantNotFound_ShouldReturnFalse()
    {
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Entities.Tenant?)null);

        var result = await CreateSut().Handle(
            new UpdateProfileSettingsCommand(Guid.NewGuid(), "Test", null), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateAndReturnTrue()
    {
        var tenant = new MesTech.Domain.Entities.Tenant { Name = "Old" };
        _tenantRepo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await CreateSut().Handle(
            new UpdateProfileSettingsCommand(tenant.Id, "MesTech Ltd", "9876543210"), CancellationToken.None);

        result.Should().BeTrue();
        tenant.Name.Should().Be("MesTech Ltd");
        tenant.TaxNumber.Should().Be("9876543210");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
