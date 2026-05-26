using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Notifications.Api.Services
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=notifications.db";
        }

        public void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createNotificationsTable = @"
                CREATE TABLE IF NOT EXISTS Notificaciones (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UsuarioId INTEGER NOT NULL,
                    Mensaje TEXT NOT NULL,
                    Tipo TEXT NOT NULL,
                    Estado TEXT NOT NULL,
                    FechaEnvio TEXT NOT NULL
                );";

            using var command = connection.CreateCommand();
            command.CommandText = createNotificationsTable;
            command.ExecuteNonQuery();
        }
    }
}