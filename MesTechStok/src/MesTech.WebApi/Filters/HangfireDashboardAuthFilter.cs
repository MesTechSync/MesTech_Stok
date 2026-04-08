using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace MesTech.WebApi.Filters;

/// <summary>
/// Hangfire Dashboard auth filter — production ortamında dashboard erişimini kısıtlar.
/// Docker/Coolify context'te tüm container'lar "local" görünür, bu yüzden
/// LocalRequestsOnly yeterli değildir.
///
/// Kullanım:
///   app.UseHangfireDashboard("/hangfire", new DashboardOptions
///   {
///       Authorization = new[] { new HangfireDashboardAuthFilter(app.Configuration) }
///   });
/// </summary>
public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string? _allowedApiKey;

    public HangfireDashboardAuthFilter(IConfiguration configuration)
    {
        _allowedApiKey = configuration["Hangfire:DashboardApiKey"];
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Development ortamında her zaman izin ver
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
            return true;

        // Production: API key ile doğrula (X-Hangfire-Key header veya query param)
        if (string.IsNullOrEmpty(_allowedApiKey))
            return false; // API key tanımlı değilse dashboard kapalı

        var headerKey = httpContext.Request.Headers["X-Hangfire-Key"].FirstOrDefault();
        var queryKey = httpContext.Request.Query["key"].FirstOrDefault();

        var providedKey = headerKey ?? queryKey;

        if (string.IsNullOrEmpty(providedKey))
            return false;

        // Constant-time comparison via SHA256 hash — prevents length-based timing leak.
        // FixedTimeEquals returns false immediately on different lengths, leaking key length.
        // Hashing both values produces fixed 32-byte arrays regardless of input length.
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(_allowedApiKey));
        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }
}
