using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orders.Api.Services
{
    public class SqliteHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        
        public SqliteHealthCheck(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db";
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

                
                return HealthCheckResult.Healthy("Conexión exitosa a la base de datos SQLite (orders.db).");
            }
            catch (Exception ex)
            {
                
                return HealthCheckResult.Unhealthy(
                    description: "No se pudo establecer comunicación con la base de datos SQLite.",
                    exception: ex);
            }
        }
    }
}