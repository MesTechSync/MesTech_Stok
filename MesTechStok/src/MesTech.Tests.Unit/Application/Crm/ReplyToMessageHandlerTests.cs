using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmMessages")]
public class ReplyToMessageHandlerTests
{
    private static PlatformMessage MakeMessage()
    {
        return new PlatformMessage
        {
            TenantId = Guid.NewGuid(),
            Platform = PlatformType.Trendyol,
            ExternalMessageId = "EXT-001",
            SenderName = "Ahmet",
            Subject = "Kargo nerede?",
            Body = "Siparisim 3 gundur gelmedi.",
            Direction = MessageDirection.Incoming,
            ReceivedAt = DateTime.UtcNow.AddHours(-2)
        };
    }

    [Fact]
    public async Task Handle_ValidReply_ShouldSetReplyAndSave()
    {
        // Arrange
        var message = MakeMessage();
        var mockRepo = new Mock<IPlatformMessageRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new ReplyToMessageHandler(mockRepo.Object, mockUow.Object);
        var command = new ReplyToMessageCommand(message.Id, "Kargonuz yolda.", "Operator1");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        message.Reply.Should().Be("Kargonuz yolda.");
        message.RepliedBy.Should().Be("Operator1");
        message.Status.Should().Be(MessageStatus.Replied);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MessageNotFound_ShouldThrow()
    {
        // Arrange
        var mockRepo = new Mock<IPlatformMessageRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformMessage?)null);

        var handler = new ReplyToMessageHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());
        var command = new ReplyToMessageCommand(Guid.NewGuid(), "Test reply", "Admin");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_EmptyReply_ShouldThrowFromDomainValidation()
    {
        // Arrange
        var message = MakeMessage();
        var mockRepo = new Mock<IPlatformMessageRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(message.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var handler = new ReplyToMessageHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());
        var command = new ReplyToMessageCommand(message.Id, "", "Admin");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
