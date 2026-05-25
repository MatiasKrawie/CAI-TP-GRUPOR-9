using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Dapper; 

namespace WebApplication3.Data
{
    public class DatabaseInitializer


    {

        private readonly string connectionString;

       
        public DatabaseInitializer(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? "Data Source=app.db";
        }
        
        public void Initialize()


        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Usamos Execute de Dapper para crear la tabla
            connection.Execute("""
                CREATE TABLE IF NOT EXISTS products (
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