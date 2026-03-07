using FluentAssertions;
using MesTech.Tests.Integration.Fixtures;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MesTech.Tests.Integration.Messaging;

/// <summary>
/// RabbitMQ publish/consume cycle tests via Testcontainers.
/// Verifies exchange/queue setup, message publish, and consumer receive with real RabbitMQ 3.
/// Uses RabbitMQ.Client v7 async API.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public class RabbitMqPublishConsumeTests : IClassFixture<RabbitMqContainerFixture>
{
    private readonly RabbitMqContainerFixture _fixture;

    public RabbitMqPublishConsumeTests(RabbitMqContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAndConsume_ShouldDeliverMessage()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_fixture.ConnectionString) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var exchange = "mestech.test.exchange";
        var queue = "mestech.test.queue";
        var routingKey = "stock.changed";

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Direct, durable: false, autoDelete: true);
        await channel.QueueDeclareAsync(queue, durable: false, exclusive: false, autoDelete: true);
        await channel.QueueBindAsync(queue, exchange, routingKey);

        // Publish
        var messageBody = Encoding.UTF8.GetBytes("{\"productId\":\"123\",\"newStock\":45}");
        await channel.BasicPublishAsync(exchange, routingKey, messageBody);

        // Consume via BasicGet (polling, simpler for tests)
        await Task.Delay(500); // brief wait for message delivery
        var result = await channel.BasicGetAsync(queue, autoAck: true);

        result.Should().NotBeNull("message should be in the queue");
        var body = Encoding.UTF8.GetString(result!.Body.ToArray());
        body.Should().Contain("\"productId\":\"123\"");
        body.Should().Contain("\"newStock\":45");
    }

    [Fact]
    public async Task EmptyQueue_BasicGet_ShouldReturnNull()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_fixture.ConnectionString) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var queue = $"mestech.empty.{Guid.NewGuid():N}";
        await channel.QueueDeclareAsync(queue, durable: false, exclusive: true, autoDelete: true);

        var result = await channel.BasicGetAsync(queue, autoAck: true);

        result.Should().BeNull("no messages were published");
    }
}
