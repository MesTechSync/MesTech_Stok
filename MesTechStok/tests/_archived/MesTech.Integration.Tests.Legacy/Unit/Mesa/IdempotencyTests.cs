using FluentAssertions;
using MesTech.Infrastructure.Messaging;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Mesa;

/// <summary>
/// Idempotency guard testleri.
/// İ-13 S-10: InMemoryProcessedMessageStore dogrulama.
/// </summary>
public class IdempotencyTests
{
    private readonly InMemoryProcessedMessageStore _store;

    public IdempotencyTests()
    {
        _store = new InMemoryProcessedMessageStore();
    }

    [Fact]
    public async Task FirstProcess_NewMessageId_ReturnsNotProcessed()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var isProcessed = await _store.IsProcessedAsync(messageId);

        // Assert
        isProcessed.Should().BeFalse("new message should not be marked as processed");
    }

    [Fact]
    public async Task DuplicateSkip_SameMessageIdTwice_SecondReturnsProcessed()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        await _store.MarkProcessedAsync(messageId, "TestConsumer");
        var isProcessed = await _store.IsProcessedAsync(messageId);

        // Assert
        isProcessed.Should().BeTrue("same message ID should be detected as duplicate");
    }

    [Fact]
    public async Task DifferentIdProcessed_DifferentMessageIds_BothIndependent()
    {
        // Arrange
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();

        // Act
        await _store.MarkProcessedAsync(messageId1, "TestConsumer");
        var isProcessed1 = await _store.IsProcessedAsync(messageId1);
        var isProcessed2 = await _store.IsProcessedAsync(messageId2);

        // Assert
        isProcessed1.Should().BeTrue("first message was marked as processed");
        isProcessed2.Should().BeFalse("second message is a different ID — not processed");
    }

    [Fact]
    public async Task MultipleConsumers_SameMessageId_AllDetectDuplicate()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        await _store.MarkProcessedAsync(messageId, "ConsumerA");
        var isProcessedByB = await _store.IsProcessedAsync(messageId);
        var isProcessedByC = await _store.IsProcessedAsync(messageId);

        // Assert
        isProcessedByB.Should().BeTrue("message ID is global — any consumer should detect it");
        isProcessedByC.Should().BeTrue("message ID is global — any consumer should detect it");
    }
}
