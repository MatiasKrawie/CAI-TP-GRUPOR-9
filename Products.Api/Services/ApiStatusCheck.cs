using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace Products.API.Services
{
    public class ApiStatusCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            
            bool isApiRunningWell = true;

            if (isApiRunningWell)
            {
                return Task.FromResult(HealthCheckResult.Healthy("El sistema principal de Products.API está operativo."));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("El sistema principal reporta fallas internas."));
        }
    }
}