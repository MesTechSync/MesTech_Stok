using Serilog.Core;
using Serilog.Events;

namespace MesTechStok.Desktop.Diagnostics
{
    /// <summary>
    /// Enriches log events with CorrelationId from Core CorrelationContext.
    /// </summary>
    public sealed class CorrelationIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var corrId = MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId;
            var prop = propertyFactory.CreateProperty("CorrelationId", corrId);
            logEvent.AddOrUpdateProperty(prop);
        }
    }
}
