using System.Threading.Tasks;

namespace MesTechStok.Core.Interfaces
{
    /// <summary>
    /// Telemetri servisinin ana interface'i
    /// API çağrıları ve circuit breaker durumlarını loglar
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// API çağrısını telemetri sistemine loglar
        /// </summary>
        /// <param name="endpoint">API endpoint'i</param>
        /// <param name="method">HTTP metodu</param>
        /// <param name="success">Başarılı olup olmadığı</param>
        /// <param name="statusCode">HTTP status kodu</param>
        /// <param name="durationMs">Süre (milisaniye)</param>
        /// <param name="category">Hata kategorisi (None, Network, Timeout, Auth, Validation)</param>
        /// <param name="correlationId">İlişkilendirme ID'si</param>
        Task LogApiCallAsync(string endpoint, string method, bool success, int statusCode,
            int durationMs, string category, string correlationId);

        /// <summary>
        /// Circuit breaker durum değişikliğini loglar
        /// </summary>
        /// <param name="previousState">Önceki durum</param>
        /// <param name="newState">Yeni durum</param>
        /// <param name="reason">Değişim sebebi</param>
        /// <param name="failureRate">Hata oranı</param>
        /// <param name="correlationId">İlişkilendirme ID'si</param>
        Task LogCircuitStateChangeAsync(string previousState, string newState, string reason,
            double failureRate, string correlationId);
    }
}
