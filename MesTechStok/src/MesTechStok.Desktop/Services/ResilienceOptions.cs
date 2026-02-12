using System.Collections.Generic;

namespace MesTechStok.Desktop.Services
{
    public class ResilienceOptions
    {
        public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
        public RetryOptions Retry { get; set; } = new();
        public int QueueRetentionHours { get; set; } = 24;
    }

    public class CircuitBreakerOptions
    {
        public double FailRateThreshold { get; set; } = 0.2;
        public int SlidingWindowSeconds { get; set; } = 60;
        public int OpenStateDurationSeconds { get; set; } = 120;
        public int HalfOpenMaxCalls { get; set; } = 10;
        public int MinimumThroughput { get; set; } = 20;
    }

    public class RetryOptions
    {
        public List<int> BackoffSeconds { get; set; } = new() { 1, 2, 4, 8, 16 };
    }
}


