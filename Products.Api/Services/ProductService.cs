using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Products.API.DTOs;
using Products.API.Models;
using Products.API.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

namespace Products.API.Services
{
    public class ProductService : IProductService
    {
        private readonly string _connectionString;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _ordersUrl;

        public ProductService(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=products.db";
            _clientFactory = clientFactory;
            _ordersUrl = configuration["OrdersServiceUrl"] ?? "https://localhost:7040";
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

            query.Append(" ORDER BY created_at DESC");

            var rows = await conn.QueryAsync<dynamic>(query.ToString(), parameters);
            var products = new List<Product>();

            foreach (var row in rows)
            {
                products.Add(new Product
                {
                    Id = Guid.Parse((string)row.id),
                    Name = (string)row.name,
                    Description = row.description?.ToString(),
                    Price = Convert.ToDouble(row.price),
                    Stock = Convert.ToInt32(row.stock),
                    Category = row.category?.ToString(),
                    CreatedAt = (string)row.CreatedAt
                });
            }

            return products;
        }

        public async Task<Product> GetByIdAsync(Guid id)
        {
            using var conn = CreateConnection();

            var row = await conn.QueryFirstOrDefaultAsync<dynamic>("""
                SELECT id, name, description, price, stock, category, created_at AS CreatedAt
                FROM products WHERE id = @Id
            """, new { Id = id.ToString() });

            if (row == null)
            {
                throw new NotFoundException("PRD-001", "Producto no encontrado.");
            }

            return new Product
            {
                Id = Guid.Parse((string)row.id),
                Name = (string)row.name,
                Description = row.description?.ToString(),
                Price = Convert.ToDouble(row.price),
                Stock = Convert.ToInt32(row.stock),
                Category = row.category?.ToString(),
                CreatedAt = (string)row.CreatedAt
            };
        }

        public async Task<Product> CreateAsync(CreateProductRequest request)
        {
            using var conn = CreateConnection();

            var existeDuplicado = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM products WHERE name = @Name AND category = @Category",
                new { request.Name, request.Category });

            if (existeDuplicado > 0)
            {
                throw new BusinessRuleException("PRD-003", "Ya existe un producto con ese nombre en la categoría.");
            }

            var nuevoId = Guid.NewGuid();
            var fechaActual = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            var parametros = new DynamicParameters();
            parametros.Add("Id", nuevoId.ToString());
            parametros.Add("Name", request.Name);
            parametros.Add("Description", request.Description);
            parametros.Add("Price", request.Price);
            parametros.Add("Stock", request.Stock);
            parametros.Add("Category", request.Category);
            parametros.Add("CreatedAt", fechaActual);

            await conn.ExecuteAsync("""
                INSERT INTO products (id, name, description, price, stock, category, created_at)
                VALUES (@Id, @Name, @Description, @Price, @Stock, @Category, @CreatedAt);
            """, parametros);

            return await GetByIdAsync(nuevoId);
        }

        public async Task<Product> UpdateAsync(Guid id, UpdateProductRequest request)
        {
            using var conn = CreateConnection();

            var parametros = new DynamicParameters();
            parametros.Add("Name", request.Name);
            parametros.Add("Description", request.Description);
            parametros.Add("Price", request.Price);
            parametros.Add("Stock", request.Stock);
            parametros.Add("Category", request.Category);
            parametros.Add("Id", id.ToString());

            var filasAfectadas = await conn.ExecuteAsync("""
                UPDATE products 
                SET name = @Name, 
                    description = @Description, 
                    price = @Price, 
                    stock = @Stock,
                    category = @Category
                WHERE id = @Id
            """, parametros);

            if (filasAfectadas == 0)
            {
                throw new NotFoundException("PRD-001", "Producto no encontrado.");
            }

            return await GetByIdAsync(id);
        }

        public async Task UpdateStockAsync(Guid id, int nuevoStock)
        {
            using var conn = CreateConnection();
            if (conn.State == ConnectionState.Closed) conn.Open();

            string sql = "UPDATE products SET stock = @Stock WHERE id = @Id;";
            int filasAfectadas = await conn.ExecuteAsync(sql, new { Stock = nuevoStock, Id = id.ToString() });

            if (filasAfectadas == 0)
            {
                throw new NotFoundException("PRD-001", $"No se encontró el producto con ID {id} para actualizar su stock.");
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var client = _clientFactory.CreateClient("OrdersClient");
            HttpResponseMessage response;

            try
            {
                response = await client.GetAsync($"{_ordersUrl}/api/orders/internal/check-product/{id}");
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException("PRD-007", $"Error crítico de comunicación con Orders.Api: {ex.Message}");
            }

            if (response.IsSuccessStatusCode)
            {
                bool tieneOrdenes = await response.Content.ReadFromJsonAsync<bool>();

                if (tieneOrdenes)
                {
                    throw new BusinessRuleException("PRD-004", "El producto tiene órdenes activas y no puede eliminarse.");
                }
            }
            else
            {
                throw new BusinessRuleException("PRD-008", $"No se pudo verificar el estado del producto en el módulo de órdenes (Status: {response.StatusCode}). Operación cancelada.");
            }

            using var conn = CreateConnection();
            if (conn.State == ConnectionState.Closed) conn.Open();

            var filasAfectadas = await conn.ExecuteAsync("DELETE FROM products WHERE id = @Id", new { Id = id.ToString() });

            if (filasAfectadas == 0)
            {
                throw new NotFoundException("PRD-001", $"No se encontró el producto con ID {id} para eliminar.");
            }

            return filasAfectadas > 0;
        }
    }
}