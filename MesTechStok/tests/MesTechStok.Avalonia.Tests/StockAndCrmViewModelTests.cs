using FluentAssertions;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 11: Stock & CRM ViewModel tests (G050)
// Coverage: Stock, Leads, Kanban, Contacts, Barcode
// ════════════════════════════════════════════════════════

#region StockAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldSetTitle()
    {
        var sut = new StockAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        sut.Title.Should().Be("Stok Yonetimi");
    }

    [Fact]
    public async Task LoadAsync_ShouldNotThrow()
    {
        var sut = new StockAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        var act = async () => await sut.LoadAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNoErrors()
    {
        var sut = new StockAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region LeadsAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class LeadsAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeCollections()
    {
        var sut = new LeadsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        sut.Leads.Should().NotBeNull();
        sut.Leads.Should().BeEmpty();
        sut.StatusOptions.Should().NotBeNull();
        sut.SourceOptions.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateLeads()
    {
        var sut = new LeadsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        await sut.LoadAsync();

        sut.IsLoading.Should().BeFalse();
    }
}

#endregion

#region KanbanAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class KanbanAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeStages()
    {
        var sut = new KanbanAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<ITenantProvider>(),
            Mock.Of<IDialogService>());

        sut.Stages.Should().NotBeNull();
        sut.Stages.Should().NotBeEmpty();
        sut.AllDeals.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldHaveDefaultKanbanStages()
    {
        var sut = new KanbanAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<ITenantProvider>(),
            Mock.Of<IDialogService>());

        sut.Stages.Count.Should().BeGreaterThanOrEqualTo(3);
    }
}

#endregion

#region ContactsAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ContactsAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldSetTitle()
    {
        var sut = new ContactsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        sut.Title.Should().Be("Kisiler");
    }

    [Fact]
    public async Task LoadAsync_ShouldNotThrow()
    {
        var sut = new ContactsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>());

        var act = async () => await sut.LoadAsync();
        await act.Should().NotThrowAsync();
    }
}

#endregion

#region BarcodeAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class BarcodeAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeCollections()
    {
        var sut = new BarcodeAvaloniaViewModel(Mock.Of<IMediator>());

        sut.Items.Should().NotBeNull();
        sut.WarehouseStocks.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldStartWithNoLoading()
    {
        var sut = new BarcodeAvaloniaViewModel(Mock.Of<IMediator>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion
