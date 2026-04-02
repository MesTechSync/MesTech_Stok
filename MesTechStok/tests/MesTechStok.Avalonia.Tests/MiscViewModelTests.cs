using FluentAssertions;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 11: Miscellaneous ViewModel tests (G050)
// Coverage: Calendar, Expenses, ReportDashboard, Billing,
//           Backup, DocumentManager, Shipment, Export,
//           Campaign, Quotation, Buybox, Marketplaces, LogViewer
// ════════════════════════════════════════════════════════

#region CalendarAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CalendarAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new CalendarAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldNotThrow()
    {
        var sut = new CalendarAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());

        var act = async () => await sut.LoadAsync();
        await act.Should().NotThrowAsync();
    }
}

#endregion

#region ExpensesAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ExpensesAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new ExpensesAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region ReportDashboardAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ReportDashboardAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new ReportDashboardAvaloniaViewModel(
            Mock.Of<ISender>(),
            Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region BillingAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class BillingAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new BillingAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region BackupAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class BackupAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new BackupAvaloniaViewModel(Mock.Of<ISender>(), Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region DocumentManagerAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DocumentManagerAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new DocumentManagerAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>(), Mock.Of<IDialogService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region ShipmentAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ShipmentAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new ShipmentAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region ExportAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ExportAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new ExportAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ITenantProvider>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region CampaignAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CampaignAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new CampaignAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ITenantProvider>(),
            Mock.Of<ILogger<CampaignAvaloniaViewModel>>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region QuotationAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class QuotationAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new QuotationAvaloniaViewModel(Mock.Of<IMediator>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region BuyboxAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class BuyboxAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new BuyboxAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region MarketplacesAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class MarketplacesAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new MarketplacesAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>(), Mock.Of<IDialogService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region LogViewerAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class LogViewerAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new LogViewerAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion
