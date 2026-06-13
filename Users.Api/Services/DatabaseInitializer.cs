using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;

namespace Users.Api.Services
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=users.db";
        }

       
            public void Initialize()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                
                conn.Execute(@"
            CREATE TABLE IF NOT EXISTS Usuarios (
                Id TEXT PRIMARY KEY, 
                Nombre TEXT NOT NULL,
                Apellido TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                FechaRegistro TEXT NOT NULL,
                Activo INTEGER NOT NULL DEFAULT 1,
                IntentosFallidos INTEGER NOT NULL DEFAULT 0,
                BloqueoFraude INTEGER NOT NULL DEFAULT 0
            );
        ");
            }
            catch (Exception ex)
            {
                
                Log.Error(ex, "Error fatal al inicializar la base de datos de Usuarios: {Message}", ex.Message);

                
                throw;
            }
        }
    }
}