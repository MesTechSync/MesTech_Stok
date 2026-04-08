using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.SaveErpSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Application.Settings;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SaveErpSettingsHandlerTests
{
    private readonly Mock<ICompanySettingsRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private SaveErpSettingsHandler CreateSut() => new(
        _repo.Object, _uow.Object, NullLogger<SaveErpSettingsHandler>.Instance);

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var act = () => CreateSut().Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NoExistingSettings_CreatesNew()
    {
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);

        var cmd = new SaveErpSettingsCommand(_tenantId, ErpProvider.Parasut, true, false, 30, 60);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.Is<CompanySettings>(s =>
            s.TenantId == _tenantId && s.ErpProvider == ErpProvider.Parasut &&
            s.AutoSyncStock == true && s.StockSyncPeriodMinutes == 30),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingSettings_Updates()
    {
        var existing = new CompanySettings { TenantId = _tenantId, ErpProvider = ErpProvider.None };
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var cmd = new SaveErpSettingsCommand(_tenantId, ErpProvider.Parasut, true, true, 15, 30);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.ErpProvider.Should().Be(ErpProvider.Parasut);
        existing.AutoSyncStock.Should().BeTrue();
        existing.StockSyncPeriodMinutes.Should().Be(15);
        _repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepoThrows_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var cmd = new SaveErpSettingsCommand(_tenantId, ErpProvider.Parasut, false, false, 30, 60);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DB down");
    }
}
