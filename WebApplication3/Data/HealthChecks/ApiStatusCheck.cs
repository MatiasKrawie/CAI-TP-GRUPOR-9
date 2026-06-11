using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication3.Data
{
    public class ApiStatusCheck : IHealthCheck
    {
       
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
           
            return Task.FromResult(
                HealthCheckResult.Healthy("La API está operativa y respondiendo peticiones HTTP con éxito.")
            );
        }
    }
}