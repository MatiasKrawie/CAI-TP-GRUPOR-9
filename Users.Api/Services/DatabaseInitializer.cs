using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

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
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Usuarios (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Apellido TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    FechaRegistro TEXT NOT NULL,
                    Activo INTEGER NOT NULL DEFAULT 1,
                    IntentosFallidos INTEGER NOT NULL DEFAULT 0,
                    BloqueoFraude INTEGER NOT NULL DEFAULT 0
                );";

            using var command = connection.CreateCommand();
            command.CommandText = createUsersTable;
            command.ExecuteNonQuery();
        }
    }
}