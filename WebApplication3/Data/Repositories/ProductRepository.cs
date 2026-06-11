using Dapper;
using global::WebApplication3.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Net;
using YamlDotNet.Core.Tokens;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebApplication3.Data.Repositories
{

        public class ProductRepository
        {

        private readonly string connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? "Data Source=app.db";
        }

        private IDbConnection CreateConnection()
        {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        }


        public async Task<IEnumerable<Product>> GetAllAsync()
            {
                using var conn = CreateConnection();
                return await conn.QueryAsync<Product>("""
        SELECT id, name, description, price, stock,
               created_at AS CreatedAt, updated_at AS UpdatedAt
        FROM products ORDER BY id DESC
    """);
            }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Product>("""
                SELECT id, name, description, price, stock,
                       created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM products WHERE id = @Id
            """, new { Id = id });
        }


        public async Task<Product> CreateAsync(CreateProductRequest request)
            {
                using var conn = CreateConnection();
                var id = await conn.ExecuteScalarAsync<int>("""
        INSERT INTO products (name, description, price, stock)
        VALUES (@Name, @Description, @Price, @Stock);
        SELECT last_insert_rowid();
    """, request);
                return (await GetByIdAsync(id))!;
            }


        public async Task<Product?> UpdateAsync(int id, UpdateProductRequest request)
        {
            using var conn = CreateConnection();

            var filasAfectadas = await conn.ExecuteAsync("""
        UPDATE products 
        SET name = @Name, 
            description = @Description, 
            price = @Price, 
            stock = @Stock,
            updated_at = datetime('now')
        WHERE id = @Id
    """, new
            {
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                Id = id
            });

            if (filasAfectadas == 0)
            {
                return null;
            }

            return await GetByIdAsync(id);
        }

        public async Task<Product?> DeleteAsync(int id)
                {
                    using var conn = CreateConnection();

                     var itemAEliminar = await GetByIdAsync(id);
                    
                    if (itemAEliminar == null)
                    {
                        return null;
                    }
                  
                    await conn.ExecuteAsync("""
                DELETE FROM products 
                WHERE id = @Id
            """, new { Id = id });

                    
                    return itemAEliminar;
                }

    }
}
