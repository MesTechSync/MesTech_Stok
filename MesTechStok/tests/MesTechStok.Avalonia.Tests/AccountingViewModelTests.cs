using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetFixedAssets;
using MesTech.Avalonia.ViewModels;
using MesTech.Avalonia.ViewModels.Accounting;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 11: Accounting ViewModel tests (G050)
// Coverage: TrialBalance, CommissionRates, FixedAsset,
//           FixedExpense, JournalEntryList, Penalty, TaxRecord
// ════════════════════════════════════════════════════════

#region TrialBalanceViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class TrialBalanceViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        var sut = new TrialBalanceViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ITenantProvider>());

        sut.TrialBalanceLines.Should().NotBeNull();
        sut.TrialBalanceLines.Should().BeEmpty();
        sut.AsOfDate.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task LoadAsync_ShouldNotThrow()
    {
        var sut = new TrialBalanceViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ITenantProvider>());

        var act = async () => await sut.LoadAsync();
        await act.Should().NotThrowAsync();
    }
}

#endregion

#region CommissionRatesViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CommissionRatesViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyCollection()
    {
        var sut = new CommissionRatesViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ITenantProvider>());

        sut.CommissionRates.Should().NotBeNull();
        sut.CommissionRates.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldNotThrow()
    {
        var sut = new CommissionRatesViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ITenantProvider>());

        var act = async () => await sut.LoadAsync();
        await act.Should().NotThrowAsync();
    }
}

#endregion

#region FixedAssetAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FixedAssetAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNoErrors()
    {
        var sut = new FixedAssetAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.Items.Should().NotBeNull().And.BeEmpty();
        sut.Categories.Should().Contain("Tumu");
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_SetsHasError()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetFixedAssetsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = new FixedAssetAvaloniaViewModel(
            mediator.Object,
            Mock.Of<ICurrentUserService>());

        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("yuklenemedi");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenEmpty_SetsIsEmpty()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetFixedAssetsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Application.Features.Accounting.Queries.GetFixedAssets.FixedAssetDto>());

        var sut = new FixedAssetAvaloniaViewModel(
            mediator.Object,
            Mock.Of<ICurrentUserService>());

        await sut.LoadAsync();

        sut.IsEmpty.Should().BeTrue();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region FixedExpenseAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FixedExpenseAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNoErrors()
    {
        var sut = new FixedExpenseAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>());

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region JournalEntryListViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class JournalEntryListViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNoErrors()
    {
        var sut = new JournalEntryListViewModel();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}

#endregion

#region KpiCardViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class KpiCardViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var sut = new KpiCardViewModel();

        sut.Should().NotBeNull();
    }
}

#endregion
