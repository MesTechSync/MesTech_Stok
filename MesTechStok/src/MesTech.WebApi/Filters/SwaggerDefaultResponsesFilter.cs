using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MesTech.WebApi.Filters;

/// <summary>
/// Swagger OperationFilter — tüm endpoint'lere ortak hata response'larını ekler.
/// 401 Unauthorized, 429 Too Many Requests, 500 Internal Server Error
/// otomatik olarak Swagger UI'da görünür. DEV6-TUR7 Röntgen borcu kapama.
/// </summary>
public sealed class SwaggerDefaultResponsesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 401 Unauthorized — JWT veya API key eksik/geçersiz
        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized — Geçersiz veya eksik API key / JWT token"
        });

        // 429 Too Many Requests — rate limit aşıldı
        operation.Responses.TryAdd("429", new OpenApiResponse
        {
            Description = "Too Many Requests — Rate limit aşıldı. Retry-After header'ına bakın."
        });

        // 500 Internal Server Error — beklenmeyen hata
        operation.Responses.TryAdd("500", new OpenApiResponse
        {
            Description = "Internal Server Error — Beklenmeyen sunucu hatası (RFC 7807 ProblemDetails)"
        });
    }
}
