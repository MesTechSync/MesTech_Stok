using FluentAssertions;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Filters;
using Xunit;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// Idempotency altyapi testleri — I-13 S-10.
/// InMemoryProcessedMessageStore davranis dogrulama.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Idempotency")]
[Trait("Phase", "I-13")]
public class IdempotencyTests
{
    // Test 1: New message is processed
    [Fact]
    public async Task InMemoryStore_NewMessageId_ReturnsNotProcessed()
    {
        var store = new InMemoryProcessedMessageStore();
        var messageId = Guid.NewGuid();

        var result = await store.IsProcessedAsync(messageId);
        result.Should().BeFalse();
    }

    // Test 2: After marking, message is detected as processed
    [Fact]
    public async Task InMemoryStore_AfterMarkProcessed_ReturnsProcessed()
    {
        var store = new InMemoryProcessedMessageStore();
        var messageId = Guid.NewGuid();

        await store.MarkProcessedAsync(messageId, "TestConsumer");

        var result = await store.IsProcessedAsync(messageId);
        result.Should().BeTrue();
    }

    // Test 3: Different message IDs are independent
    [Fact]
    public async Task InMemoryStore_DifferentMessageIds_AreIndependent()
    {
        var store = new InMemoryProcessedMessageStore();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await store.MarkProcessedAsync(id1, "TestConsumer");

        (await store.IsProcessedAsync(id1)).Should().BeTrue();
        (await store.IsProcessedAsync(id2)).Should().BeFalse();
    }

    // Test 4: Multiple consumers can mark the same ID (idempotent)
    [Fact]
    public async Task InMemoryStore_DoubleMarkSameId_NoException()
    {
        var store = new InMemoryProcessedMessageStore();
        var messageId = Guid.NewGuid();

        await store.MarkProcessedAsync(messageId, "Consumer1");
        await store.MarkProcessedAsync(messageId, "Consumer2"); // should not throw

        (await store.IsProcessedAsync(messageId)).Should().BeTrue();
    }
}
