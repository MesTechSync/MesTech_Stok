using System.Net;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Runtime;

/// <summary>
/// OpenCart multi-instance parallel sync tests.
/// Validates 3-instance concurrent operation, batch stock updates,
/// delta sync with orders, and concurrent connection tests.
/// </summary>
public class OpenCartMultiInstanceTests
{
    private static readonly string ProductsResponseTemplate =
        """
        {{
            "data": [
                {{"name": "{0}", "sku": "{1}", "price": "29.90", "quantity": "100", "description": "Test product"}}
            ],
            "total": 1
        }}
        """;

    private static OpenCartAdapter CreateAdapter(
        MockHttpMessageHandler handler, string baseUrl)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl)
        };
        var logger = new Mock<ILogger<OpenCartAdapter>>();
        return new OpenCartAdapter(httpClient, logger.Object);
    }

    private static Dictionary<string, string> MakeCredentials(string baseUrl) => new()
    {
        ["BaseUrl"] = baseUrl,
        ["ApiToken"] = $"token-{Guid.NewGuid():N}"
    };

    private static async Task ConfigureAdapter(
        OpenCartAdapter adapter, MockHttpMessageHandler handler, string baseUrl)
    {
        // Enqueue a successful TestConnection response
        handler.EnqueueResponse(HttpStatusCode.OK,
            """{"data": [{"name": "Test"}], "total": 5}""");

        var result = await adapter.TestConnectionAsync(MakeCredentials(baseUrl));
        Assert.True(result.IsSuccess);
    }

    // ── Test 1: Three instances parallel sync ────────────────────────────

    [Fact]
    public async Task ThreeInstances_ParallelSync_AllSucceed()
    {
        // Arrange — 3 OpenCartAdapter instances with different mock handlers
        var handler1 = new MockHttpMessageHandler();
        var handler2 = new MockHttpMessageHandler();
        var handler3 = new MockHttpMessageHandler();

        var adapter1 = CreateAdapter(handler1, "https://shop1.example.com/");
        var adapter2 = CreateAdapter(handler2, "https://shop2.example.com/");
        var adapter3 = CreateAdapter(handler3, "https://shop3.example.com/");

        // Configure each adapter via TestConnectionAsync
        await ConfigureAdapter(adapter1, handler1, "https://shop1.example.com/");
        await ConfigureAdapter(adapter2, handler2, "https://shop2.example.com/");
        await ConfigureAdapter(adapter3, handler3, "https://shop3.example.com/");

        // Enqueue PullProducts responses with unique products
        handler1.EnqueueResponse(HttpStatusCode.OK,
            string.Format(ProductsResponseTemplate, "Product-Shop1", "SKU-SHOP1-001"));
        handler2.EnqueueResponse(HttpStatusCode.OK,
            string.Format(ProductsResponseTemplate, "Product-Shop2", "SKU-SHOP2-001"));
        handler3.EnqueueResponse(HttpStatusCode.OK,
            string.Format(ProductsResponseTemplate, "Product-Shop3", "SKU-SHOP3-001"));

        // Act — pull products from all 3 in parallel
        var tasks = new[]
        {
            adapter1.PullProductsAsync(),
            adapter2.PullProductsAsync(),
            adapter3.PullProductsAsync()
        };

        var results = await Task.WhenAll(tasks);

        // Assert — all 3 return products, no exceptions
        Assert.Equal(3, results.Length);

        Assert.NotEmpty(results[0]);
        Assert.Equal("Product-Shop1", results[0][0].Name);
        Assert.Equal("SKU-SHOP1-001", results[0][0].SKU);

        Assert.NotEmpty(results[1]);
        Assert.Equal("Product-Shop2", results[1][0].Name);
        Assert.Equal("SKU-SHOP2-001", results[1][0].SKU);

        Assert.NotEmpty(results[2]);
        Assert.Equal("Product-Shop3", results[2][0].Name);
        Assert.Equal("SKU-SHOP3-001", results[2][0].SKU);
    }

    // ── Test 2: Batch stock update with SemaphoreSlim(5) ─────────────────

    [Fact]
    public async Task BatchStockUpdate_SemaphoreSlim5_Concurrent()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var adapter = CreateAdapter(handler, "https://batch-test.example.com/");

        // Configure adapter (1 TestConnection request)
        await ConfigureAdapter(adapter, handler, "https://batch-test.example.com/");

        // Enqueue 20 OK responses for 20 stock updates
        for (var i = 0; i < 20; i++)
        {
            handler.EnqueueResponse(HttpStatusCode.OK, """{"success": true}""");
        }

        // Build 20 stock update items
        var updates = Enumerable.Range(1, 20)
            .Select(i => (ProductId: Guid.NewGuid(), NewStock: i * 10))
            .ToList()
            .AsReadOnly();

        // Act
        var successCount = await adapter.PushBatchStockUpdateAsync(updates);

        // Assert — all 20 should succeed
        Assert.Equal(20, successCount);

        // Verify total requests: 1 (TestConnection) + 20 (stock updates)
        Assert.Equal(21, handler.CapturedRequests.Count);

        // Verify all stock update requests are PUT requests
        var stockUpdateRequests = handler.CapturedRequests.Skip(1).ToList();
        Assert.All(stockUpdateRequests, req =>
            Assert.Equal(HttpMethod.Put, req.Method));
    }

    // ── Test 3: Delta sync with orders since date ────────────────────────

    [Fact]
    public async Task DeltaSync_SinceDate_CorrectBehavior()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var adapter = CreateAdapter(handler, "https://delta-test.example.com/");

        // Configure adapter
        await ConfigureAdapter(adapter, handler, "https://delta-test.example.com/");

        // Enqueue orders response with 2 orders — 1 recent, 1 old
        var recentDate = DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-dd HH:mm:ss");
        var oldDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd HH:mm:ss");

        handler.EnqueueResponse(HttpStatusCode.OK,
            $$"""
            {
                "data": [
                    {
                        "order_id": 1001,
                        "order_status_id": "2",
                        "firstname": "Ali",
                        "lastname": "Veli",
                        "email": "ali@test.com",
                        "total": "150.00",
                        "currency_code": "TRY",
                        "date_added": "{{recentDate}}"
                    },
                    {
                        "order_id": 1000,
                        "order_status_id": "5",
                        "firstname": "Mehmet",
                        "lastname": "Demir",
                        "email": "mehmet@test.com",
                        "total": "250.00",
                        "currency_code": "TRY",
                        "date_added": "{{oldDate}}"
                    }
                ]
            }
            """);

        // Cast to IOrderCapableAdapter
        var orderAdapter = adapter as IOrderCapableAdapter;
        Assert.NotNull(orderAdapter);

        // Act — pull orders since 1 day ago (should filter out the 5-day-old order)
        var sinceDate = DateTime.UtcNow.AddDays(-1);
        var orders = await orderAdapter.PullOrdersAsync(since: sinceDate);

        // Assert — only the recent order should be returned
        Assert.NotNull(orders);
        Assert.Single(orders);
        Assert.Equal("1001", orders[0].PlatformOrderId);
        Assert.Equal("OpenCart", orders[0].PlatformCode);
        Assert.Equal("Processing", orders[0].Status);
        Assert.Equal("Ali Veli", orders[0].CustomerName);

        // Verify the request was made to the orders endpoint
        var orderRequest = handler.CapturedRequests.Last();
        Assert.Contains("/api/rest/orders", orderRequest.RequestUri!.ToString());
    }

    // ── Test 4: Three instances concurrent connections ────────────────────

    [Fact]
    public async Task ThreeInstances_ConcurrentConnections_NoDeadlock()
    {
        // Arrange — 3 separate handlers and adapters
        var handler1 = new MockHttpMessageHandler();
        var handler2 = new MockHttpMessageHandler();
        var handler3 = new MockHttpMessageHandler();

        var adapter1 = CreateAdapter(handler1, "https://concurrent1.example.com/");
        var adapter2 = CreateAdapter(handler2, "https://concurrent2.example.com/");
        var adapter3 = CreateAdapter(handler3, "https://concurrent3.example.com/");

        // Enqueue test connection responses
        handler1.EnqueueResponse(HttpStatusCode.OK,
            """{"data": [{"name": "P1"}], "total": 10}""");
        handler2.EnqueueResponse(HttpStatusCode.OK,
            """{"data": [{"name": "P2"}], "total": 20}""");
        handler3.EnqueueResponse(HttpStatusCode.OK,
            """{"data": [{"name": "P3"}], "total": 30}""");

        // Act — all 3 TestConnectionAsync calls in parallel
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var tasks = new[]
        {
            adapter1.TestConnectionAsync(MakeCredentials("https://concurrent1.example.com/"), cts.Token),
            adapter2.TestConnectionAsync(MakeCredentials("https://concurrent2.example.com/"), cts.Token),
            adapter3.TestConnectionAsync(MakeCredentials("https://concurrent3.example.com/"), cts.Token)
        };

        var results = await Task.WhenAll(tasks);

        // Assert — all 3 succeed, no deadlock (completed within 10 seconds)
        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.True(r.IsSuccess));

        Assert.Equal(10, results[0].ProductCount);
        Assert.Equal(20, results[1].ProductCount);
        Assert.Equal(30, results[2].ProductCount);

        // Each handler captured exactly 1 request
        Assert.Single(handler1.CapturedRequests);
        Assert.Single(handler2.CapturedRequests);
        Assert.Single(handler3.CapturedRequests);
    }
}
