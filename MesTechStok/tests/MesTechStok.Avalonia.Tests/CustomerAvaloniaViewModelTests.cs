using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CustomerAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private CustomerAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCustomersCrmQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCustomersCrmResult { Items = [], TotalCount = 0 });
        return new CustomerAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.SearchText.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenEmpty_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Items.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
        sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Ahmet";

        // Assert
        sut.Items.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldNotDoubleAdd()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert — should not double-add
        sut.Items.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.HasError.Should().BeFalse();
    }
}
