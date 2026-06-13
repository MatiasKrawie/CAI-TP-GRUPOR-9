using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks; 
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Users.Api.Services
{
   
    public class SqliteHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public SqliteHealthCheck(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=users.db";
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                if (conn.State == ConnectionState.Open)
                {
                    return HealthCheckResult.Healthy("Conexión exitosa a la base de datos de Usuarios SQLite.");
                }

                return HealthCheckResult.Unhealthy("No se pudo establecer la conexión con la base de datos SQLite.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Falla crítica en SQLite: {ex.Message}");
            }
        }
    }
}