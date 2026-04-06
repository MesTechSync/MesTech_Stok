using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetCrmActivities;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ActivityAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private ActivityAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCrmActivitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrmActivitiesResult { Activities = [] });
        return new(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
        sut.Activities.Should().BeEmpty();
        sut.FilterOptions.Should().HaveCount(6);
        sut.SelectedFilter.Should().BeNull();
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
        sut.Activities.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
        sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public void ActivityTypeIcons_ShouldResolveCorrectly()
    {
        // Assert — verify TypeIcon logic via DTO directly
        var call = new ActivityItemVm { Type = "Arama" };
        call.TypeIcon.Should().Be("T");

        var email = new ActivityItemVm { Type = "E-posta" };
        email.TypeIcon.Should().Be("@");

        var meeting = new ActivityItemVm { Type = "Toplanti" };
        meeting.TypeIcon.Should().Be("M");
    }

    [Fact]
    public async Task LoadAsync_3StateTransition_LoadingToEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — final 3-state: not loading, no error, empty (no mock data)
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }
}
