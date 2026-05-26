using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Api.Services
{
    public class ApiStatusCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
   
            return Task.FromResult(HealthCheckResult.Healthy("El microservicio de Órdenes está operativo y respondiendo peticiones."));
        }
    }
}