using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PlatformMessagesAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IDialogService> _dialogMock = new();

    private PlatformMessagesAvaloniaViewModel CreateSut() => new(_mediatorMock.Object, _dialogMock.Object);

    private GetPlatformMessagesResult CreateResult(int count = 2)
    {
        var items = Enumerable.Range(0, count).Select(i => new PlatformMessageDto
        {
            Id = Guid.NewGuid(),
            Platform = "Trendyol",
            SenderName = $"Sender {i}",
            Subject = $"Subject {i}",
            BodyPreview = $"Body {i}",
            Status = "Unread",
            Direction = "Incoming",
            HasAiSuggestion = i == 0,
            AiSuggestedReply = i == 0 ? "AI suggestion" : null,
            ReceivedAt = DateTime.Now.AddMinutes(-i * 30)
        }).ToList();

        return new GetPlatformMessagesResult { Items = items, TotalCount = count };
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
        sut.Messages.Should().BeEmpty();
        sut.SelectedStatusFilter.Should().Be("Tumu");
        sut.SelectedPlatformFilter.Should().Be("Tumu");
        sut.ReplyText.Should().BeEmpty();
        sut.IsSendingReply.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_Success_ShouldPopulateMessages()
    {
        // Arrange
        var sut = CreateSut();
        var result = CreateResult(3);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPlatformMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Messages.Should().HaveCount(3);
        sut.TotalCount.Should().Be(3);
        sut.IsEmpty.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
        sut.SelectedMessage.Should().NotBeNull();
        sut.SelectedMessage!.SenderName.Should().Be("Sender 0");
    }

    [Fact]
    public async Task LoadAsync_EmptyResult_ShouldSetIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPlatformMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPlatformMessagesResult { Items = [], TotalCount = 0 });

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Messages.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.IsEmpty.Should().BeTrue();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_Exception_ShouldSetErrorState()
    {
        // Arrange
        var sut = CreateSut();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPlatformMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        // Act
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("DB down");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void UseAiSuggestionCommand_ShouldCopyAiReplyToReplyText()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedMessage = new PlatformMessageItemVm
        {
            Id = Guid.NewGuid(),
            AiSuggestedReply = "Merhaba, siparisiz hazirlaniyor."
        };

        // Act
        sut.UseAiSuggestionCommand.Execute(null);

        // Assert
        sut.ReplyText.Should().Be("Merhaba, siparisiz hazirlaniyor.");
    }
}
