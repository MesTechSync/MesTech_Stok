using FluentAssertions;
using MesTech.Avalonia.ViewModels;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class EInvoiceAvaloniaViewModelTests
{
    private readonly EInvoiceAvaloniaViewModel _sut;

    public EInvoiceAvaloniaViewModelTests()
    {
        _sut = new EInvoiceAvaloniaViewModel();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SearchText.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.Invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulate8Invoices()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Invoices.Should().HaveCount(8);
        _sut.TotalCount.Should().Be(8);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.Invoices.Should().Contain(i => i.Status == "Onaylandi");
        _sut.Invoices.Should().Contain(i => i.Status == "Beklemede");
        _sut.Invoices.Should().Contain(i => i.Status == "Iptal Edildi");
    }

    [Fact]
    public async Task SearchText_ShouldFilterByReceiverOrInvoiceNo()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Teknosa";

        // Assert
        _sut.Invoices.Should().HaveCount(1);
        _sut.Invoices[0].Receiver.Should().Contain("Teknosa");
        _sut.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateInvoiceCommand_ShouldAddNewDraftInvoice()
    {
        // Arrange
        await _sut.LoadAsync();
        var initialCount = _sut.Invoices.Count;

        // Act
        await _sut.CreateInvoiceCommand.ExecuteAsync(null);

        // Assert
        _sut.Invoices.Should().HaveCount(initialCount + 1);
        _sut.Invoices[0].Status.Should().Be("Taslak");
        _sut.Invoices[0].Amount.Should().Be(0.00m);
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingStates()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EInvoiceAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }
}
