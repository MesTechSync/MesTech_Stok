using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using Minio.DataModel.Args;

namespace MesTech.Infrastructure.HealthChecks;

public class MinioHealthCheck : IHealthCheck
{
    private readonly IMinioClient _minioClient;

    public MinioHealthCheck(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ListBuckets is the lightest MinIO operation to verify connectivity
            await _minioClient.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("MinIO baglantisi basarili");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO baglantisi basarisiz", ex);
        }
    }
}
