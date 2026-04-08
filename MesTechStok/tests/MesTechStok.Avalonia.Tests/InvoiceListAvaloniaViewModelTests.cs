using FluentAssertions;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Common;
using MesTech.Application.DTOs.EInvoice;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class InvoiceListAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly InvoiceListAvaloniaViewModel _sut;

    public InvoiceListAvaloniaViewModelTests()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetEInvoicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<EInvoiceDto>.Empty());
        _sut = new InvoiceListAvaloniaViewModel(_mediatorMock.Object, Mock.Of<IDialogService>(), Mock.Of<ITenantProvider>(), Mock.Of<INavigationService>());
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
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task FilterByType_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedType = "e-Arsiv";

        // Assert
        _sut.Invoices.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task FilterByStatus_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedStatus = "Reddedildi";

        // Assert
        _sut.Invoices.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Yilmaz";

        // Assert
        _sut.Invoices.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }
}
