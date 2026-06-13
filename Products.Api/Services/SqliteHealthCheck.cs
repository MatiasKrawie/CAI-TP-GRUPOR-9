using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Products.API.Services
{
    public class SqliteHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public SqliteHealthCheck(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? "Data Source=products.db";
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                await command.ExecuteScalarAsync(cancellationToken);

                return HealthCheckResult.Healthy("La base de datos SQLite responde correctamente.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos SQLite.", ex);
            }
        }
    }
}