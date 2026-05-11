using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Products.API.HealthChecks;

public class SqliteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public SqliteHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString =
                _configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=app.db";

            using var connection = new SqliteConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            await connection.ExecuteScalarAsync<int>("SELECT 1");

            return HealthCheckResult.Healthy(
                "SQLite funcionando correctamente.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Error conectando con SQLite.",
                ex);
        }
    }
}
