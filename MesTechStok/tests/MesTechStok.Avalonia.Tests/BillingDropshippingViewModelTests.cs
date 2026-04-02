using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 1: G431 — Billing + Dropshipping VM unit tests
// BillingAvaloniaViewModel, FeedCreateAvaloniaViewModel,
// FeedPreviewAvaloniaViewModel
// SupplierFeedListAvaloniaViewModel already covered
// ════════════════════════════════════════════════════════

#region BillingAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class BillingAvaloniaViewModelFullTests
{
    private static BillingAvaloniaViewModel CreateSut(Mock<IMediator>? mediator = null)
    {
        var m = mediator ?? new Mock<IMediator>();
        return new BillingAvaloniaViewModel(m.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.TotalCount.Should().Be(0);
        sut.CurrentPlan.Should().Be("Pro");
        sut.MonthlyFee.Should().Be("0,00 TL");
        sut.NextBillingDate.Should().Be("-");
        sut.SearchText.Should().BeEmpty();
        sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BillingAvaloniaViewModel.IsLoading))
                loadingStates.Add(sut.IsLoading);
        };

        await sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenError_SetsErrorState()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection lost"));

        var sut = CreateSut(mediator);
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse();
    }
}

#endregion

#region FeedCreateAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FeedCreateAvaloniaViewModelTests
{
    private static FeedCreateAvaloniaViewModel CreateSut(Mock<IMediator>? mediator = null)
    {
        var m = mediator ?? new Mock<IMediator>();
        return new FeedCreateAvaloniaViewModel(m.Object, Mock.Of<ITenantProvider>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.FeedName.Should().BeEmpty();
        sut.FeedUrl.Should().BeEmpty();
        sut.SelectedFormat.Should().Be("XML");
        sut.SyncIntervalMinutes.Should().Be(60);
        sut.PriceMarkupPercent.Should().Be(15.0m);
        sut.MinimumMarginPercent.Should().Be(5.0m);
        sut.IsSaving.Should().BeFalse();
        sut.SaveCompleted.Should().BeFalse();
        sut.FormatOptions.Should().HaveCount(4);
        sut.ColumnMappings.Should().HaveCount(5);
    }

    [Fact]
    public void Constructor_ShouldInitDefaultMappings()
    {
        var sut = CreateSut();

        sut.ColumnMappings.Should().Contain(m => m.SourceColumn == "urun_adi" && m.TargetField == "Name");
        sut.ColumnMappings.Should().Contain(m => m.SourceColumn == "fiyat" && m.TargetField == "Price");
        sut.ColumnMappings.Should().Contain(m => m.SourceColumn == "stok" && m.TargetField == "Stock");
        sut.ColumnMappings.Should().Contain(m => m.SourceColumn == "barkod" && m.TargetField == "Barcode");
        sut.ColumnMappings.Should().Contain(m => m.SourceColumn == "kategori" && m.TargetField == "Category");
    }

    [Fact]
    public async Task LoadAsync_ShouldResetForm()
    {
        var sut = CreateSut();
        sut.FeedName = "Changed";
        sut.FeedUrl = "https://changed.com";
        sut.SaveCompleted = true;

        await sut.LoadAsync();

        sut.FeedName.Should().BeEmpty();
        sut.FeedUrl.Should().BeEmpty();
        sut.SaveCompleted.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FeedCreateAvaloniaViewModel.IsLoading))
                loadingStates.Add(sut.IsLoading);
        };

        await sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
    }
}

#endregion

#region FeedPreviewAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FeedPreviewAvaloniaViewModelTests
{
    private static FeedPreviewAvaloniaViewModel CreateSut(Mock<IMediator>? mediator = null)
    {
        var m = mediator ?? new Mock<IMediator>();
        return new FeedPreviewAvaloniaViewModel(m.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.FeedUrl.Should().BeEmpty();
        sut.FeedFormat.Should().Be("XML");
        sut.FeedSourceId.Should().Be(Guid.Empty);
        sut.TotalProducts.Should().Be(0);
        sut.ValidProducts.Should().Be(0);
        sut.ErrorCount.Should().Be(0);
        sut.PreviewLoaded.Should().BeFalse();
        sut.PreviewItems.Should().BeEmpty();
        sut.ValidationErrors.Should().BeEmpty();
        sut.FormatOptions.Should().HaveCount(4);
    }

    [Fact]
    public async Task LoadAsync_ShouldResetPreviewState()
    {
        var sut = CreateSut();
        sut.PreviewLoaded = true;
        sut.TotalProducts = 50;

        await sut.LoadAsync();

        sut.PreviewLoaded.Should().BeFalse();
        sut.TotalProducts.Should().Be(0);
        sut.ValidProducts.Should().Be(0);
        sut.ErrorCount.Should().Be(0);
        sut.PreviewItems.Should().BeEmpty();
        sut.ValidationErrors.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FeedPreviewAvaloniaViewModel.IsLoading))
                loadingStates.Add(sut.IsLoading);
        };

        await sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
    }
}

#endregion
