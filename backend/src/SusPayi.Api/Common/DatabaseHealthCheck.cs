using Microsoft.Extensions.Diagnostics.HealthChecks;
using SusPayi.Infrastructure.Persistence;

namespace SusPayi.Api.Common;

/// <summary>Hazır olma kontrolü: veritabanına ulaşılamıyorsa trafik bu örneğe gönderilmemeli.</summary>
internal sealed class DatabaseHealthCheck(SusPayiDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Veritabanına bağlanılamıyor.");
}
