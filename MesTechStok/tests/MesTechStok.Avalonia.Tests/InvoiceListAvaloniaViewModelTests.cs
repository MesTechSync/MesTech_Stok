using FluentAssertions;
using MesTech.Avalonia.ViewModels;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class InvoiceListAvaloniaViewModelTests
{
    private readonly InvoiceListAvaloniaViewModel _sut;

    public InvoiceListAvaloniaViewModelTests()
    {
        _sut = new InvoiceListAvaloniaViewModel(null);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.SelectedType.Should().Be("Tumu");
        _sut.SelectedStatus.Should().Be("Tumu");
        _sut.SelectedPlatform.Should().Be("Tumu");
        _sut.CurrentPage.Should().Be(1);
        _sut.TotalPages.Should().Be(1);
        _sut.Invoices.Should().BeEmpty();
        _sut.InvoiceTypes.Should().Contain("e-Fatura");
        _sut.StatusList.Should().Contain("Reddedildi");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateInvoices()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Invoices.Should().HaveCount(5);
        _sut.TotalCount.Should().Be(5);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.Invoices.Should().Contain(i => i.Type == "e-Fatura");
        _sut.Invoices.Should().Contain(i => i.Platform == "Trendyol");
    }

    [Fact]
    public async Task FilterByType_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedType = "e-Arsiv";

        // Assert
        _sut.Invoices.Should().HaveCount(1);
        _sut.Invoices[0].Type.Should().Be("e-Arsiv");
    }

    [Fact]
    public async Task FilterByStatus_Reddedildi_ShouldReturnSingleItem()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedStatus = "Reddedildi";

        // Assert
        _sut.Invoices.Should().HaveCount(1);
        _sut.Invoices[0].RecipientName.Should().Contain("Arslan");
    }

    [Fact]
    public async Task SearchText_ShouldFilterByRecipientOrInvoiceNumber()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Yilmaz";

        // Assert
        _sut.Invoices.Should().HaveCount(1);
        _sut.Invoices[0].InvoiceNumber.Should().Be("MES2026000001");
    }
}
