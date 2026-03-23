using FluentAssertions;
using MesTech.Avalonia.ViewModels;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class InvoiceManagementAvaloniaViewModelTests
{
    private readonly InvoiceManagementAvaloniaViewModel _sut;

    public InvoiceManagementAvaloniaViewModelTests()
    {
        _sut = new InvoiceManagementAvaloniaViewModel();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SelectedType.Should().Be("Tumu");
        _sut.SearchText.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.Invoices.Should().BeEmpty();
        _sut.InvoiceTypes.Should().HaveCount(3);
        _sut.InvoiceTypes.Should().Contain("e-Fatura");
        _sut.InvoiceTypes.Should().Contain("e-Arsiv");
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
        _sut.Invoices.Should().Contain(i => i.Durum == "Onayli");
        _sut.Invoices.Should().Contain(i => i.Durum == "Bekliyor");
        _sut.Invoices.Should().Contain(i => i.Durum == "Reddedildi");
    }

    [Fact]
    public async Task FilterByType_eFatura_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedType = "e-Fatura";

        // Assert — 5 e-Fatura items
        _sut.Invoices.Should().HaveCount(5);
        _sut.Invoices.Should().OnlyContain(i => i.Tip == "e-Fatura");
        _sut.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task SearchText_ShouldFilterByAliciOrFaturaNo()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Demir";

        // Assert
        _sut.Invoices.Should().HaveCount(1);
        _sut.Invoices[0].Alici.Should().Contain("Demir Bilisim");
    }

    [Fact]
    public async Task CreateInvoiceCommand_ShouldAddNewInvoiceToList()
    {
        // Arrange
        await _sut.LoadAsync();
        var initialCount = _sut.Invoices.Count;

        // Act
        await _sut.CreateInvoiceCommand.ExecuteAsync(null);

        // Assert
        _sut.Invoices.Should().HaveCount(initialCount + 1);
        _sut.Invoices[0].Durum.Should().Be("Bekliyor");
        _sut.Invoices[0].Tutar.Should().Be(0.00m);
        _sut.Invoices[0].Tip.Should().Be("e-Fatura");
    }
}
