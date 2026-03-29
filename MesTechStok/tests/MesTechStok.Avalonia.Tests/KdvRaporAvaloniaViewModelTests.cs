using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class KdvRaporAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly KdvRaporAvaloniaViewModel _sut;

    public KdvRaporAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new KdvRaporAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.SalesVat.Should().Contain("0,00 TL");
        _sut.PurchaseVat.Should().Contain("0,00 TL");
        _sut.NetVat.Should().Contain("0,00 TL");
        _sut.SelectedPeriodType.Should().Be("Aylik");
        _sut.SelectedInvoiceType.Should().Be("Tumu");
        _sut.PeriodTypes.Should().HaveCount(3);
        _sut.InvoiceTypes.Should().HaveCount(4);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateKPIsAndItems()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — salesVat=22586, purchaseVat=14120, net=8466
        _sut.SalesVat.Should().Contain("22");
        _sut.PurchaseVat.Should().Contain("14");
        _sut.NetVat.Should().Contain("8");
        _sut.Items.Should().HaveCount(6);
        _sut.IsLoading.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task FilterByInvoiceType_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedInvoiceType = "Satis Faturasi";

        // Assert
        _sut.Items.Should().HaveCount(3);
        _sut.Items.Should().OnlyContain(x => x.InvoiceType == "Satis Faturasi");
    }

    [Fact]
    public async Task FilterByInvoiceType_Iade_ShouldReturnSingleItem()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedInvoiceType = "Iade Faturasi";

        // Assert
        _sut.Items.Should().HaveCount(1);
        _sut.Items[0].InvoiceNumber.Should().Contain("IAD");
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingStates()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(KdvRaporAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_SetsHasError()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetKdvReportQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var sut = new KdvRaporAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse(); // KÇ-13
    }
}
