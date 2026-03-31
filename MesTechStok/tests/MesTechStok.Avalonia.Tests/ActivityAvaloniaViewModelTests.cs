using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ActivityAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private ActivityAvaloniaViewModel CreateSut() => new(_mediatorMock.Object, Mock.Of<ICurrentUserService>());

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
        sut.Activities.Should().BeEmpty();
        sut.FilterOptions.Should().HaveCount(6);
        sut.SelectedFilter.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulate4Activities()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Activities.Should().HaveCount(4);
        sut.TotalCount.Should().Be(4);
        sut.IsEmpty.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ActivitiesShouldHaveCorrectTypes()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var types = sut.Activities.Select(a => a.Type).ToList();
        types.Should().Contain("Arama");
        types.Should().Contain("E-posta");
        types.Should().Contain("Toplanti");
        types.Should().Contain("Not");
    }

    [Fact]
    public async Task LoadAsync_ActivityTypeIconsShouldResolve()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var call = sut.Activities.First(a => a.Type == "Arama");
        call.TypeIcon.Should().Be("T");

        var email = sut.Activities.First(a => a.Type == "E-posta");
        email.TypeIcon.Should().Be("@");

        var meeting = sut.Activities.First(a => a.Type == "Toplanti");
        meeting.TypeIcon.Should().Be("M");
    }

    [Fact]
    public async Task LoadAsync_3StateTransition_LoadingToSuccess()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — final 3-state: not loading, no error, not empty
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsEmpty.Should().BeFalse();
        sut.Activities.Should().AllSatisfy(a =>
        {
            a.Subject.Should().NotBeNullOrEmpty();
            a.CreatedBy.Should().NotBeNullOrEmpty();
        });
    }
}
