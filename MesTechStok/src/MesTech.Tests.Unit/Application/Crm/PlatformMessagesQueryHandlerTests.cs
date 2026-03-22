using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmMessages")]
public class PlatformMessagesQueryHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private static PlatformMessage MakeMessage(string body = "Test body", string sender = "Customer")
    {
        return new PlatformMessage
        {
            TenantId = _tenantId,
            Platform = PlatformType.Trendyol,
            ExternalMessageId = $"EXT-{Guid.NewGuid():N}",
            SenderName = sender,
            Subject = "Test Subject",
            Body = body,
            Direction = MessageDirection.Incoming,
            ReceivedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Handle_WithMessages_ShouldReturnMappedDtos()
    {
        // Arrange
        var messages = new List<PlatformMessage>
        {
            MakeMessage("Short body", "Ali"),
            MakeMessage("Another message body", "Veli")
        }.AsReadOnly();

        var mockRepo = new Mock<IPlatformMessageRepository>();
        mockRepo.Setup(r => r.GetPagedAsync(_tenantId, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((messages, 2));

        var handler = new GetPlatformMessagesHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(
            new GetPlatformMessagesQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].SenderName.Should().Be("Ali");
        result.Items[0].Platform.Should().Be("Trendyol");
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_LongBody_ShouldTruncatePreview()
    {
        // Arrange
        var longBody = new string('A', 200);
        var message = MakeMessage(longBody);
        var mockRepo = new Mock<IPlatformMessageRepository>();
        mockRepo.Setup(r => r.GetPagedAsync(_tenantId, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<PlatformMessage> { message }.AsReadOnly(), 1));

        var handler = new GetPlatformMessagesHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(
            new GetPlatformMessagesQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Items[0].BodyPreview.Should().HaveLength(123); // 120 chars + "..."
        result.Items[0].BodyPreview.Should().EndWith("...");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = new GetPlatformMessagesHandler(Mock.Of<IPlatformMessageRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
