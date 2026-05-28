using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using Products.API.DTOs;
using Products.API.Models;
using Products.API.Exceptions;
using Products.API.Services;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Products.API.Services
{
    public class ProductService : IProductService
    {
        private readonly string _connectionString;

        public ProductService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=products.db";
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        // GET /api/products con filtros dinámicos
        public async Task<IEnumerable<Product>> GetAllAsync(string? category, string? name)
        {
            using var conn = CreateConnection();

            var query = new StringBuilder("SELECT id, name, description, price, stock, category, created_at AS CreatedAt FROM products WHERE 1=1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(category))
            {
                query.Append(" AND category = @Category");
                parameters.Add("Category", category);
            }

            if (!string.IsNullOrEmpty(name))
            {
                query.Append(" AND name LIKE @Name");
                parameters.Add("Name", $"%{name}%"); 
            }

            query.Append(" ORDER BY id DESC");

            return await conn.QueryAsync<Product>(query.ToString(), parameters);
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            var product = await conn.QueryFirstOrDefaultAsync<Product>("""
                SELECT id, name, description, price, stock, category, created_at AS CreatedAt
                FROM products WHERE id = @Id
            """, new { Id = id });

            // PRD-001: Si el ID no existe
            if (product == null)
            {
                throw new ProductException("PRD-001", 404, "Producto no encontrado.");
            }

            return product;
        }

        public async Task<Product> CreateAsync(CreateProductRequest request)
        {
            using var conn = CreateConnection();

            // PRD-003: Ya existe un producto con ese nombre en la categoría
            var existeDuplicado = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM products WHERE name = @Name AND category = @Category",
                new { request.Name, request.Category });

            if (existeDuplicado > 0)
            {
                throw new ProductException("PRD-003", 409, "Ya existe un producto con ese nombre en la categoría.");
            }


            var fechaActual = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var id = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO products (name, description, price, stock, category, created_at)
                VALUES (@Name, @Description, @Price, @Stock, @Category, @CreatedAt);
                SELECT last_insert_rowid();
            """, new
            {
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                request.Category,
                CreatedAt = fechaActual
            });


            return await GetByIdAsync(id);
        }

        public async Task<Product> UpdateAsync(int id, UpdateProductRequest request)
        {
            using var conn = CreateConnection();
            var filasAfectadas = await conn.ExecuteAsync("""
                UPDATE products 
                SET name = @Name, 
                    description = @Description, 
                    price = @Price, 
                    stock = @Stock,
                    category = @Category
                WHERE id = @Id
            """, new
            {
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                request.Category,
                Id = id
            });
            if (filasAfectadas == 0)
            {
                // PRD-001: Producto no encontrado al intentar un PUT
                throw new ProductException("PRD-001", 404, "Producto no encontrado.");
            }

            return await GetByIdAsync(id);
        }

        //PUT STOCK
        public async Task UpdateStockAsync(int id, int nuevoStock)
        {
            using var conn = CreateConnection(); 
            if (conn.State == ConnectionState.Closed) conn.Open();

           
            string sql = "UPDATE products SET stock = @Stock WHERE Id = @Id;";

            int filasAfectadas = await conn.ExecuteAsync(sql, new { Stock = nuevoStock, Id = id });

            
            if (filasAfectadas == 0)
            {
                throw new ProductException("PRD-001", 404, $"No se encontró el producto con ID {id} para actualizar su stock.");
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = CreateConnection();


            var tieneOrdenes = await conn.ExecuteScalarAsync<int>(
                 "SELECT COUNT(1) FROM Ordenes WHERE product_id = @Id", new { Id = id });

            if (tieneOrdenes > 0)
            {
                throw new ProductException("PRD-004", 409, "El producto tiene órdenes activas y no puede eliminarse.");
            }

            var filasAfectadas = await conn.ExecuteAsync("DELETE FROM products WHERE id = @Id", new { Id = id });

            

            // PRD-004: El producto tiene órdenes activas
            return filasAfectadas > 0;
        }
    }
}