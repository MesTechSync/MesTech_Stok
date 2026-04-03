using FluentAssertions;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Common;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class InvoiceManagementAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly InvoiceManagementAvaloniaViewModel _sut;

    public InvoiceManagementAvaloniaViewModelTests()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetInvoicesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<InvoiceListDto>());
        _sut = new InvoiceManagementAvaloniaViewModel(_mediatorMock.Object);
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
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Invoices.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.IsEmpty.Should().BeTrue();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InvoiceManagementAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }
}
