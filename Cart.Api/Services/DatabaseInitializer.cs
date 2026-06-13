using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog; 
using System;

namespace Cart.Api.Services
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=cart.db";
        }

        public void Initialize()
        {
            Log.Information("Iniciando la verificación e inicialización de la base de datos del Carrito...");

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                var createCartTable = @"
                    CREATE TABLE IF NOT EXISTS Carritos (
                        UsuarioId TEXT PRIMARY KEY,
                        FechaActualizacion TEXT NOT NULL DEFAULT (datetime('now'))
                    );";

                var createCartDetailsTable = @"
                    CREATE TABLE IF NOT EXISTS CarritoDetalles (
                        UsuarioId TEXT,
                        ProductoId TEXT,
                        Cantidad INTEGER NOT NULL,
                        PRIMARY KEY (UsuarioId, ProductoId),
                        FOREIGN KEY (UsuarioId) REFERENCES Carritos(UsuarioId) ON DELETE CASCADE
                    );";

                using var command = connection.CreateCommand();

                command.CommandText = createCartTable;
                command.ExecuteNonQuery();

                command.CommandText = createCartDetailsTable;
                command.ExecuteNonQuery();

                Log.Information("Base de datos del Carrito inicializada correctamente con soporte para GUID.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error crítico al intentar inicializar la base de datos de SQLite del Carrito.");
                throw;
            }
        }
    }
}