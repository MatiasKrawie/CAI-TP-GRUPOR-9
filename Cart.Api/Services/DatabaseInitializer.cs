using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

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
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createCartTable = @"
                CREATE TABLE IF NOT EXISTS Carritos (
                    UsuarioId INTEGER PRIMARY KEY,
                    FechaActualizacion TEXT NOT NULL
                );";

            var createCartDetailsTable = @"
                CREATE TABLE IF NOT EXISTS CarritoDetalles (
                    UsuarioId INTEGER,
                    ProductoId INTEGER,
                    Cantidad INTEGER NOT NULL,
                    PRIMARY KEY (UsuarioId, ProductoId),
                    FOREIGN KEY (UsuarioId) REFERENCES Carritos(UsuarioId) ON DELETE CASCADE
                );";

            using var command = connection.CreateCommand();
            command.CommandText = createCartTable;
            command.ExecuteNonQuery();

            command.CommandText = createCartDetailsTable;
            command.ExecuteNonQuery();
        }
    }
}