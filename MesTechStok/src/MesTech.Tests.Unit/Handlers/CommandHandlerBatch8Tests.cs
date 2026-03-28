using FluentAssertions;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Application.Features.Crm.Commands.UpdateDealStage;
using MesTech.Application.Commands.UpdateExpense;
using MesTech.Application.Commands.UpdateIncome;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Application.Commands.UpdateProductImage;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Features.Product.Commands.ValidateBulkImport;
using MesTech.Application.Features.Auth.Commands.VerifyTotp;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
[Trait("Phase", "TUR7")]
public class CommandHandlerBatch8Tests
{
    [Fact]
    public async Task UpdateCariHesap_NullRequest_Throws()
    {
        var sut = new UpdateCariHesapHandler(
            Mock.Of<ICariHesapRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateDealStage_NullRequest_Throws()
    {
        var sut = new UpdateDealStageHandler(
            Mock.Of<ICrmDealRepository>(), Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<UpdateDealStageHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateExpense_NullRequest_Throws()
    {
        var sut = new UpdateExpenseHandler(
            Mock.Of<IExpenseRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateIncome_NullRequest_Throws()
    {
        var sut = new UpdateIncomeHandler(
            Mock.Of<IIncomeRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateNotificationSettings_NullRequest_Throws()
    {
        var sut = new UpdateNotificationSettingsHandler(
            Mock.Of<INotificationSettingRepository>(), Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<UpdateNotificationSettingsHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateProductImage_NullRequest_Throws()
    {
        var sut = new UpdateProductImageHandler(
            Mock.Of<IProductRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateTenant_NullRequest_Throws()
    {
        var sut = new UpdateTenantHandler(
            Mock.Of<ITenantRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateWarehouse_NullRequest_Throws()
    {
        var sut = new UpdateWarehouseHandler(
            Mock.Of<IWarehouseRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateBulkImport_NullRequest_Throws()
    {
        var sut = new ValidateBulkImportHandler(Mock.Of<IBulkProductImportService>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task VerifyTotp_NullRequest_Throws()
    {
        var sut = new VerifyTotpHandler(
            Mock.Of<IUserRepository>(), Mock.Of<ITotpService>(),
            Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<VerifyTotpHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
