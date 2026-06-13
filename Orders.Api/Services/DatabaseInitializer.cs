using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;

namespace Orders.Api.Services
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db";
        }

        public void Initialize()
        {
            Log.Information("Iniciando la verificación e inicialización de la base de datos de Órdenes...");

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var createOrdersTable = @"
                    CREATE TABLE IF NOT EXISTS Ordenes (
                        Id TEXT PRIMARY KEY,
                        UsuarioId TEXT NOT NULL, 
                        Total DECIMAL(18,2) NOT NULL,
                        Estado TEXT NOT NULL,
                        FechaCreacion TEXT NOT NULL
                    );";

                var createOrderItemsTable = @"
                    CREATE TABLE IF NOT EXISTS OrdenDetalles (
                        OrdenId TEXT NOT NULL,
                        ProductoId TEXT NOT NULL, 
                        Cantidad INTEGER NOT NULL,
                        PrecioUnitario DECIMAL(18,2) NOT NULL,
                        PRIMARY KEY (OrdenId, ProductoId), 
                        FOREIGN KEY (OrdenId) REFERENCES Ordenes(Id) ON DELETE CASCADE 
                    );";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createOrdersTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createOrderItemsTable;
                    command.ExecuteNonQuery();
                }

                Log.Information("Base de datos de Órdenes inicializada con IDs basados en GUID.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error crítico al inicializar la base de datos de Órdenes.");
                throw;
            }
        }
    }
}