using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PipelineAvaloniaViewModelTests
{
    private static PipelineAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new PipelineAvaloniaViewModel(mediatorMock.Object);
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
    public async Task LoadAsync_ShouldPopulateStages()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Stages.Should().HaveCount(4);
        sut.TotalCount.Should().Be(20);
        sut.TotalValue.Should().Be("421.000 TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldContainExpectedStageData()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Stages.Should().Contain(s => s.Name == "Ilk Iletisim" && s.DealCount == 8);
        sut.Stages.Should().Contain(s => s.Name == "Kazanildi" && s.Percentage == 32);
        sut.Stages.Should().Contain(s => s.Color == "#8B5CF6");
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldClearAndReload()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert — should not double-add, collection cleared each time
        sut.Stages.Should().HaveCount(4);
        sut.TotalCount.Should().Be(20);
        sut.HasError.Should().BeFalse();
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
        sut.Stages.Should().HaveCount(4);
        sut.TotalValue.Should().Be("421.000 TL");
    }
}
