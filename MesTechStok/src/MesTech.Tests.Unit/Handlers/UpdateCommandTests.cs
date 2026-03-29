using FluentAssertions;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Application.Commands.UpdateExpense;
using MesTech.Application.Commands.UpdateIncome;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for 11 Update command handlers: CariHesap, Counterparty, Expense, Income,
/// FixedAsset, FixedExpense, NotificationSettings, PlatformCommissionRate,
/// ProductImage, ProfileSettings, Tenant.
/// </summary>
[Trait("Category", "Unit")]
public class UpdateCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _id = Guid.NewGuid();

    // ═══════ UpdateCariHesapHandler ═══════

    [Fact]
    public async Task UpdateCariHesap_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICariHesapRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdateCariHesapHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCariHesap_NotFound_ReturnsFalse()
    {
        var repo = new Mock<ICariHesapRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((CariHesap?)null);

        var sut = new UpdateCariHesapHandler(repo.Object, uow.Object);
        var cmd = new UpdateCariHesapCommand(_id, "Test", null, CariHesapType.Musteri, null, null, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCariHesap_Found_ReturnsTrue()
    {
        var repo = new Mock<ICariHesapRepository>();
        var uow = new Mock<IUnitOfWork>();
        var entity = new CariHesap { Name = "Old" };
        repo.Setup(r => r.GetByIdAsync(_id)).ReturnsAsync(entity);

        var sut = new UpdateCariHesapHandler(repo.Object, uow.Object);
        var cmd = new UpdateCariHesapCommand(_id, "New", "123", CariHesapType.Musteri, "555", "a@b.com", "Addr");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        entity.Name.Should().Be("New");
        repo.Verify(r => r.UpdateAsync(entity), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ UpdateCounterpartyHandler ═══════

    [Fact]
    public async Task UpdateCounterparty_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICounterpartyRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdateCounterpartyHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCounterparty_NotFound_ReturnsFalse()
    {
        var repo = new Mock<ICounterpartyRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Counterparty?)null);

        var sut = new UpdateCounterpartyHandler(repo.Object, uow.Object);
        var cmd = new UpdateCounterpartyCommand(_id, "Test");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    // ═══════ UpdateExpenseHandler ═══════

    [Fact]
    public async Task UpdateExpense_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Expense?)null);

        var sut = new UpdateExpenseHandler(repo.Object, uow.Object);
        var cmd = new UpdateExpenseCommand(_id);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateExpense_ValidRequest_CallsUpdateAndSave()
    {
        var repo = new Mock<IExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var expense = new Expense { Description = "Test", ExpenseType = ExpenseType.Kargo };
        expense.SetAmount(100m);
        repo.Setup(r => r.GetByIdAsync(_id)).ReturnsAsync(expense);

        var sut = new UpdateExpenseHandler(repo.Object, uow.Object);
        var cmd = new UpdateExpenseCommand(_id, Description: "Updated");

        await sut.Handle(cmd, CancellationToken.None);

        expense.Description.Should().Be("Updated");
        repo.Verify(r => r.UpdateAsync(expense), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ UpdateIncomeHandler ═══════

    [Fact]
    public async Task UpdateIncome_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IIncomeRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Income?)null);

        var sut = new UpdateIncomeHandler(repo.Object, uow.Object);
        var cmd = new UpdateIncomeCommand(_id);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateIncome_ValidRequest_CallsUpdateAndSave()
    {
        var repo = new Mock<IIncomeRepository>();
        var uow = new Mock<IUnitOfWork>();
        var income = new Income { Description = "Test", IncomeType = IncomeType.Satis };
        income.SetAmount(500m);
        repo.Setup(r => r.GetByIdAsync(_id)).ReturnsAsync(income);

        var sut = new UpdateIncomeHandler(repo.Object, uow.Object);
        var cmd = new UpdateIncomeCommand(_id, Description: "Updated");

        await sut.Handle(cmd, CancellationToken.None);

        income.Description.Should().Be("Updated");
        repo.Verify(r => r.UpdateAsync(income), Times.Once);
    }

    // ═══════ UpdateFixedAssetHandler ═══════

    [Fact]
    public async Task UpdateFixedAsset_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedAsset?)null);

        var sut = new UpdateFixedAssetHandler(repo.Object, uow.Object);
        var cmd = new UpdateFixedAssetCommand(_id, _tenantId, "CNC", null, 5);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateFixedAsset_ValidRequest_ReturnsUnitValue()
    {
        var repo = new Mock<IFixedAssetRepository>();
        var uow = new Mock<IUnitOfWork>();
        var asset = FixedAsset.Create(
            _tenantId, "Old", "253", 10000m, DateTime.UtcNow.AddYears(-1), 5,
            MesTech.Domain.Accounting.Enums.DepreciationMethod.StraightLine);
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        var sut = new UpdateFixedAssetHandler(repo.Object, uow.Object);
        var cmd = new UpdateFixedAssetCommand(_id, _tenantId, "NewName", "Desc", 10);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        repo.Verify(r => r.UpdateAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ UpdateFixedExpenseHandler ═══════

    [Fact]
    public async Task UpdateFixedExpense_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedExpense?)null);

        var sut = new UpdateFixedExpenseHandler(repo.Object, uow.Object);
        var cmd = new UpdateFixedExpenseCommand(_id, MonthlyAmount: 500m);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateFixedExpense_ValidRequest_UpdatesAndSaves()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var expense = FixedExpense.Create(_tenantId, "Rent", 1000m, 15, DateTime.UtcNow);
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var sut = new UpdateFixedExpenseHandler(repo.Object, uow.Object);
        var cmd = new UpdateFixedExpenseCommand(_id, MonthlyAmount: 1500m);

        await sut.Handle(cmd, CancellationToken.None);

        repo.Verify(r => r.UpdateAsync(expense, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ UpdateNotificationSettingsHandler ═══════

    [Fact]
    public async Task UpdateNotificationSettings_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<INotificationSettingRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<UpdateNotificationSettingsHandler>>();
        var sut = new UpdateNotificationSettingsHandler(repo.Object, uow.Object, logger.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateNotificationSettings_NewSetting_CreatesAndReturnsGuid()
    {
        var repo = new Mock<INotificationSettingRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<UpdateNotificationSettingsHandler>>();
        repo.Setup(r => r.GetByUserAndChannelAsync(It.IsAny<Guid>(), It.IsAny<NotificationChannel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationSetting?)null);

        var sut = new UpdateNotificationSettingsHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new UpdateNotificationSettingsCommand(
            _tenantId, Guid.NewGuid(), NotificationChannel.Email, "test@test.com",
            true, true, true, 10, true, true, true, true, true, true, true, true,
            null, null, "tr", false, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<NotificationSetting>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ UpdatePlatformCommissionRateHandler ═══════

    [Fact]
    public async Task UpdatePlatformCommissionRate_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdatePlatformCommissionRateHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePlatformCommissionRate_NotFound_ReturnsFalse()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformCommission?)null);

        var sut = new UpdatePlatformCommissionRateHandler(repo.Object, uow.Object);
        var cmd = new UpdatePlatformCommissionRateCommand(_id, Rate: 5.0m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePlatformCommissionRate_Found_ReturnsTrue()
    {
        var repo = new Mock<IPlatformCommissionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var commission = new PlatformCommission
        {
            TenantId = _tenantId,
            Platform = PlatformType.Trendyol,
            Rate = 10.0m,
            IsActive = true
        };
        repo.Setup(r => r.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(commission);

        var sut = new UpdatePlatformCommissionRateHandler(repo.Object, uow.Object);
        var cmd = new UpdatePlatformCommissionRateCommand(_id, Rate: 15.0m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        commission.Rate.Should().Be(15.0m);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ UpdateProductImageHandler ═══════

    [Fact]
    public async Task UpdateProductImage_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdateProductImageHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateProductImage_ProductNotFound_ReturnsFailure()
    {
        var repo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var sut = new UpdateProductImageHandler(repo.Object, uow.Object);
        var cmd = new UpdateProductImageCommand(_id, "http://img.com/test.png");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateProductImage_ProductFound_ReturnsSuccess()
    {
        var repo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var product = new Product { ImageUrl = "old.png" };
        repo.Setup(r => r.GetByIdAsync(_id)).ReturnsAsync(product);

        var sut = new UpdateProductImageHandler(repo.Object, uow.Object);
        var cmd = new UpdateProductImageCommand(_id, "http://img.com/new.png");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.ImageUrl.Should().Be("http://img.com/new.png");
    }

    // ═══════ UpdateProfileSettingsHandler ═══════

    [Fact]
    public async Task UpdateProfileSettings_TenantNotFound_ReturnsFalse()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Entities.Tenant?)null);

        var sut = new UpdateProfileSettingsHandler(repo.Object, Mock.Of<IUnitOfWork>());
        var cmd = new UpdateProfileSettingsCommand(_tenantId, "Name", "123");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProfileSettings_TenantFound_ReturnsTrue()
    {
        var repo = new Mock<ITenantRepository>();
        var tenant = new MesTech.Domain.Entities.Tenant { Name = "Old" };
        repo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var sut = new UpdateProfileSettingsHandler(repo.Object, Mock.Of<IUnitOfWork>());
        var cmd = new UpdateProfileSettingsCommand(_tenantId, "NewName", "VKN123");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        tenant.Name.Should().Be("NewName");
        tenant.TaxNumber.Should().Be("VKN123");
    }

    // ═══════ UpdateTenantHandler ═══════

    [Fact]
    public async Task UpdateTenant_TenantNotFound_ReturnsFalse()
    {
        var repo = new Mock<ITenantRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Entities.Tenant?)null);

        var sut = new UpdateTenantHandler(repo.Object, uow.Object);
        var cmd = new UpdateTenantCommand(_tenantId, "Name", null, true);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenant_TenantFound_UpdatesAndReturnsTrue()
    {
        var repo = new Mock<ITenantRepository>();
        var uow = new Mock<IUnitOfWork>();
        var tenant = new MesTech.Domain.Entities.Tenant { Name = "Old", IsActive = true };
        repo.Setup(r => r.GetByIdAsync(_tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var sut = new UpdateTenantHandler(repo.Object, uow.Object);
        var cmd = new UpdateTenantCommand(_tenantId, "NewTenant", "TAX456", false);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        tenant.Name.Should().Be("NewTenant");
        tenant.TaxNumber.Should().Be("TAX456");
        tenant.IsActive.Should().BeFalse();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
