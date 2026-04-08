using FluentAssertions;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PipelineAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private PipelineAvaloniaViewModel CreateSut()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPipelineKanbanQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KanbanBoardDto());
        return new PipelineAvaloniaViewModel(_mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());
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
        sut.TotalCount.Should().Be(0);
        sut.TotalValue.Should().Be("0 TL");
        sut.Stages.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.Stages.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.TotalValue.Should().Be("0 TL");
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldNotAccumulate()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshCommand_ShouldDelegateToLoadAsync()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}
