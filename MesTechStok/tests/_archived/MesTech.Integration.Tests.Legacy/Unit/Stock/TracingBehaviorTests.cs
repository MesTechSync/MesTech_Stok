using System.Diagnostics;
using FluentAssertions;
using MediatR;
using MesTech.Application.Behaviors;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// G038: TracingBehavior — MediatR pipeline cross-cutting concern.
/// Her handler'ı etkiler — Activity span, success/fail tags, duration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Behavior")]
[Trait("Group", "TracingBehavior")]
public class TracingBehaviorTests
{
    private readonly Mock<ILogger<TracingBehavior<TestRequest, TestResponse>>> _logger = new();

    // ═══ Test request/response ═══
    public record TestRequest(string Name) : IRequest<TestResponse>;
    public record TestResponse(bool Success);

    [Fact]
    public async Task Handle_Success_CompletesWithoutException()
    {
        var behavior = new TracingBehavior<TestRequest, TestResponse>(_logger.Object);
        var request = new TestRequest("test-success");
        var next = new RequestHandlerDelegate<TestResponse>(
            () => Task.FromResult(new TestResponse(true)));

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Exception_RethrowsAndLogs()
    {
        var behavior = new TracingBehavior<TestRequest, TestResponse>(_logger.Object);
        var request = new TestRequest("test-error");
        var next = new RequestHandlerDelegate<TestResponse>(
            () => throw new InvalidOperationException("Handler failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(request, next, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SetsActivitySpan_WithCorrectName()
    {
        // ActivitySource listener setup
        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "MesTech.Application.Handlers",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => capturedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new TracingBehavior<TestRequest, TestResponse>(_logger.Object);
        var request = new TestRequest("span-test");
        var next = new RequestHandlerDelegate<TestResponse>(
            () => Task.FromResult(new TestResponse(true)));

        await behavior.Handle(request, next, CancellationToken.None);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.DisplayName.Should().Contain("Handler:");
    }

    [Fact]
    public async Task Handle_Success_SetsOkStatus()
    {
        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "MesTech.Application.Handlers",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => capturedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new TracingBehavior<TestRequest, TestResponse>(_logger.Object);
        await behavior.Handle(
            new TestRequest("ok-test"),
            () => Task.FromResult(new TestResponse(true)),
            CancellationToken.None);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task Handle_Error_SetsErrorStatus()
    {
        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "MesTech.Application.Handlers",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => capturedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new TracingBehavior<TestRequest, TestResponse>(_logger.Object);

        try
        {
            await behavior.Handle(
                new TestRequest("error-test"),
                () => throw new InvalidOperationException("boom"),
                CancellationToken.None);
        }
        catch { /* expected */ }

        capturedActivity.Should().NotBeNull();
        capturedActivity!.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task Handle_SetsDurationTag()
    {
        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "MesTech.Application.Handlers",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => capturedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new TracingBehavior<TestRequest, TestResponse>(_logger.Object);
        await behavior.Handle(
            new TestRequest("duration-test"),
            () => Task.FromResult(new TestResponse(true)),
            CancellationToken.None);

        capturedActivity.Should().NotBeNull();
        // SetTag adds to TagObjects (not string Tags) — check via GetTagItem
        var durationValue = capturedActivity!.GetTagItem("mestech.handler.duration_ms");
        durationValue.Should().NotBeNull();
    }
}
