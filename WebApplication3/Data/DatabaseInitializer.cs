using Microsoft.Data.Sqlite;
using Dapper; // Asegúrate de instalar el paquete NuGet 'Dapper'

namespace WebApplication3.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Usamos Execute de Dapper para crear la tabla
            connection.Execute("""
                CREATE TABLE IF NOT EXISTS items (
                    id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    name        TEXT    NOT NULL,
                    description TEXT,
                    price       REAL    NOT NULL DEFAULT 0,
                    stock       INTEGER NOT NULL DEFAULT 0,
                    created_at  TEXT    NOT NULL DEFAULT (datetime('now')),
                    updated_at  TEXT
                );
            """);
        }
    }
}