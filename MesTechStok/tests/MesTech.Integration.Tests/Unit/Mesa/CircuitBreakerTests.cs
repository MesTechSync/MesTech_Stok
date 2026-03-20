using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.CircuitBreaker;

namespace MesTech.Integration.Tests.Unit.Mesa;

/// <summary>
/// Circuit breaker davranisi testleri.
/// İ-13 S-09: 5 senaryo ile circuit breaker dogrulama.
/// </summary>
public class CircuitBreakerTests
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private int _callCount;
    private bool _shouldFail;

    public CircuitBreakerTests()
    {
        _callCount = 0;
        _shouldFail = false;

        // Polly circuit breaker: 3 ardisik hata -> open, 30s sure
        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(2)); // Test icin 2s
    }

    private Task<string> SimulateCallAsync()
    {
        _callCount++;
        if (_shouldFail)
            throw new HttpRequestException("MESA OS unreachable");
        return Task.FromResult("success");
    }

    [Fact]
    public async Task HappyPath_AllSuccessful_CircuitStaysClosed()
    {
        // Arrange
        _shouldFail = false;

        // Act
        for (int i = 0; i < 5; i++)
        {
            var result = await _circuitBreaker.ExecuteAsync(SimulateCallAsync);
            result.Should().Be("success");
        }

        // Assert
        _callCount.Should().Be(5);
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task ThreeFailures_CircuitOpens_FourthCallThrowsBrokenCircuit()
    {
        // Arrange
        _shouldFail = true;

        // Act — 3 failures
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _circuitBreaker.ExecuteAsync(SimulateCallAsync));
        }

        // Assert — circuit is now open
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Open);

        // 4th call should throw BrokenCircuitException (not even attempt)
        await Assert.ThrowsAsync<BrokenCircuitException>(
            () => _circuitBreaker.ExecuteAsync(SimulateCallAsync));

        _callCount.Should().Be(3); // 4th call never executed
    }

    [Fact]
    public async Task CircuitOpen_WaitForBreak_TransitionsToHalfOpen()
    {
        // Arrange — break the circuit
        _shouldFail = true;
        for (int i = 0; i < 3; i++)
        {
            try { await _circuitBreaker.ExecuteAsync(SimulateCallAsync); } catch { }
        }
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Open);

        // Act — wait for break duration (2s in test)
        await Task.Delay(2500);

        // Assert — circuit should be half-open
        _circuitBreaker.CircuitState.Should().Be(CircuitState.HalfOpen);

        // One successful call should close it
        _shouldFail = false;
        var result = await _circuitBreaker.ExecuteAsync(SimulateCallAsync);
        result.Should().Be("success");
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public async Task FallbackOnOpen_ReturnsEmptyInsteadOfException()
    {
        // Arrange — break the circuit
        _shouldFail = true;
        for (int i = 0; i < 3; i++)
        {
            try { await _circuitBreaker.ExecuteAsync(SimulateCallAsync); } catch { }
        }

        // Act — fallback pattern
        string result;
        try
        {
            result = await _circuitBreaker.ExecuteAsync(SimulateCallAsync);
        }
        catch (BrokenCircuitException)
        {
            result = string.Empty; // Fallback
        }

        // Assert
        result.Should().BeEmpty();
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Open);
    }

    [Fact]
    public async Task MixedResults_CircuitOpensAndRecovers()
    {
        // 2 successful calls
        _shouldFail = false;
        for (int i = 0; i < 2; i++)
        {
            await _circuitBreaker.ExecuteAsync(SimulateCallAsync);
        }
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Closed);

        // 3 failures — circuit opens
        _shouldFail = true;
        for (int i = 0; i < 3; i++)
        {
            try { await _circuitBreaker.ExecuteAsync(SimulateCallAsync); } catch { }
        }
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Open);

        // Wait for recovery
        await Task.Delay(2500);

        // 1 successful call — circuit closes
        _shouldFail = false;
        await _circuitBreaker.ExecuteAsync(SimulateCallAsync);
        _circuitBreaker.CircuitState.Should().Be(CircuitState.Closed);

        _callCount.Should().Be(6); // 2 success + 3 fail + 1 recovery
    }
}
