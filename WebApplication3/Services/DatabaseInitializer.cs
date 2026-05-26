using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;

namespace Products.API.Services
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? "Data Source=products.db";
        }

        public void Initialize()
        {
            Log.Information("Iniciando la verificación e inicialización de la base de datos...");

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

               
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS products (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        description TEXT,
                        price REAL NOT NULL,
                        stock INTEGER NOT NULL,
                        category TEXT,
                        created_at DATE DEFAULT (datetime('now'))
                    );");


                Log.Information("Base de datos inicializada correctamente.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error crítico al intentar inicializar la base de datos de SQLite.");
                throw;
            }
        }
    }
}