using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using MesTech.Avalonia.ViewModels.Accounting;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 1: G407 Batch — Penalty + TaxRecord VM unit tests
// FixedAsset + FixedExpense already covered in AccountingViewModelTests.cs
// ════════════════════════════════════════════════════════

#region PenaltyAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PenaltyAvaloniaViewModelTests
{
    private static PenaltyAvaloniaViewModel CreateSut(Mock<IMediator>? mediator = null)
    {
        var m = mediator ?? new Mock<IMediator>();
        return new PenaltyAvaloniaViewModel(m.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.TotalCount.Should().Be(0);
        sut.TotalPenaltyAmount.Should().Be("0,00 TL");
        sut.PendingAmount.Should().Be("0,00 TL");
        sut.PaidAmount.Should().Be("0,00 TL");
        sut.SearchText.Should().BeEmpty();
        sut.SelectedSource.Should().Be("Tumu");
        sut.Items.Should().BeEmpty();
        sut.Sources.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PenaltyAvaloniaViewModel.IsLoading))
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
        mediator.Setup(m => m.Send(It.IsAny<GetPenaltyRecordsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Query timeout"));

        var sut = CreateSut(mediator);
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("Query timeout");
        sut.IsLoading.Should().BeFalse();
    }
}

#endregion

#region TaxRecordAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class TaxRecordAvaloniaViewModelTests
{
    private static TaxRecordAvaloniaViewModel CreateSut(Mock<IMediator>? mediator = null)
    {
        var m = mediator ?? new Mock<IMediator>();
        return new TaxRecordAvaloniaViewModel(m.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.TotalCount.Should().Be(0);
        sut.TotalTaxAmount.Should().Be("0,00 TL");
        sut.PaidTaxAmount.Should().Be("0,00 TL");
        sut.PendingTaxAmount.Should().Be("0,00 TL");
        sut.SearchText.Should().BeEmpty();
        sut.SelectedTaxType.Should().Be("Tumu");
        sut.Items.Should().BeEmpty();
        sut.TaxTypes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TaxRecordAvaloniaViewModel.IsLoading))
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
        mediator.Setup(m => m.Send(It.IsAny<GetTaxRecordsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var sut = CreateSut(mediator);
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("Service unavailable");
        sut.IsLoading.Should().BeFalse();
    }
}

#endregion
