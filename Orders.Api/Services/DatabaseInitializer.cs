using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Data;

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
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();


                        var createOrdersTable = @"
                CREATE TABLE IF NOT EXISTS Ordenes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UsuarioId INTEGER NOT NULL,
                    Total DECIMAL(18,2) NOT NULL,
                    Estado TEXT NOT NULL,
                    FechaCreacion TEXT NOT NULL
                );";


                    var createOrderItemsTable = @"
            CREATE TABLE IF NOT EXISTS OrdenDetalles (
                OrdenId INTEGER NOT NULL,
                ProductoId INTEGER NOT NULL,
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
        }
    }
}