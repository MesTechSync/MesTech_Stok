using System;
using System.Threading;

namespace MesTechStok.Core.Diagnostics
{
    /// <summary>
    /// Lightweight correlation context using AsyncLocal to propagate a correlation id across async flows.
    /// </summary>
    public static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> _current = new();
        private const int MaxIdLength = 36; // UUID length safeguard

        /// <summary>
        /// Gets current correlation id (generates one lazily if missing)
        /// </summary>
        public static string CurrentId => _current.Value ??= GenerateId();

        /// <summary>
        /// Starts a new correlation scope replacing any existing id. Previous id restored on dispose.
        /// </summary>
        public static IDisposable StartNew(string? correlationId = null)
        {
            var previous = _current.Value;
            _current.Value = Sanitize(correlationId) ?? GenerateId();
            return new Scope(previous);
        }

        private static string GenerateId() => Guid.NewGuid().ToString();

        private static string? Sanitize(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            id = id.Trim();
            if (id.Length > MaxIdLength) id = id.Substring(0, MaxIdLength);
            return id;
        }

        private sealed class Scope : IDisposable
        {
            private readonly string? _previous;
            private bool _disposed;
            public Scope(string? previous) => _previous = previous;
            public void Dispose()
            {
                if (_disposed) return;
                _current.Value = _previous;
                _disposed = true;
            }
        }
    }
}
