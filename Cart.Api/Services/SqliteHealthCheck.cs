using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Cart.Api.Services
{
    public class SqliteHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public SqliteHealthCheck(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=cart.db";
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                return HealthCheckResult.Healthy("Conexión exitosa a la base de datos de Cart SQLite.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Falla crítica en Cart SQLite: {ex.Message}");
            }
        }
    }
}