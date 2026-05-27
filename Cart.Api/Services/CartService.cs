using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Cart.Api.DTOs;
using Cart.Api.Exceptions;

namespace Cart.Api.Services
{
    public class CartService : ICartService
    {
        private readonly string _connectionString;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _productUrl;
        private readonly string _usersUrl;

        public CartService(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=cart.db";
            _clientFactory = clientFactory;
            _productUrl = configuration["ProductServiceUrl"] ?? "https://localhost:7137";
            _usersUrl = configuration["UserServiceUrl"] ?? "https://localhost:7058";
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<CartResponse> GetByUserIdAsync(int userId)
        {
            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId });

            if (!cartExists.HasValue)
                throw new NotFoundException("CRT-001", 404, "Carrito no encontrado.");

            return await GetCartResponseInternalAsync(conn, userId);
        }

        public async Task<CartResponse> AddItemAsync(int userId, CartItemRequest request)
        {
            
            if (request.Cantidad <= 0)
                throw new NotFoundException("CRT-004", 400, "Cantidad inválida.");

            await ValidateUserExistsAsync(userId);

            var product = await FetchProductFromApiAsync(request.ProductoId);

            using var conn = CreateConnection();
            if (conn.State == ConnectionState.Closed) conn.Open();

            
            var existingItem = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT Cantidad FROM CarritoDetalles WHERE UsuarioId = @UserId AND ProductoId = @ProdId",
                new { UserId = userId, ProdId = request.ProductoId });

            int nuevaCantidadTotal = request.Cantidad + (existingItem ?? 0);

            
            if (product.Stock < nuevaCantidadTotal)
                throw new NotFoundException("CRT-003", 422, $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {nuevaCantidadTotal}.");

            using var transaction = conn.BeginTransaction();
            try
            {
                var fechaActual = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                
                await conn.ExecuteAsync(@"
                    INSERT INTO Carritos (UsuarioId, FechaActualizacion)
                    VALUES (@UserId, @Fecha)
                    ON CONFLICT(UsuarioId) DO UPDATE SET FechaActualizacion = @Fecha;",
                    new { UserId = userId, Fecha = fechaActual }, transaction);

                
                await conn.ExecuteAsync(@"
                    INSERT INTO CarritoDetalles (UsuarioId, ProductoId, Cantidad)
                    VALUES (@UserId, @ProdId, @Cant)
                    ON CONFLICT(UsuarioId, ProductoId) DO UPDATE SET Cantidad = @Cant;",
                    new { UserId = userId, ProdId = request.ProductoId, Cant = nuevaCantidadTotal }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw new NotFoundException("CRT-005", 500, "Error interno al procesar el carrito.");
            }

            return await GetCartResponseInternalAsync(conn, userId);
        }

        public async Task<CartResponse> UpdateItemCantidadAsync(int userId, int productId, UpdateCantidadRequest request)
        {
            if (request.Cantidad <= 0)
                throw new NotFoundException("CRT-004", 400, "Cantidad inválida.");

            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId });

            if (!cartExists.HasValue)
                throw new NotFoundException("CRT-001", 404, "Carrito no encontrado.");

            
            var product = await FetchProductFromApiAsync(productId);
            if (product.Stock < request.Cantidad)
                throw new NotFoundException("CRT-003", 422, $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {request.Cantidad}.");

            var updatedRows = await conn.ExecuteAsync(@"
                UPDATE CarritoDetalles SET Cantidad = @Cant 
                WHERE UsuarioId = @UserId AND ProductoId = @ProdId",
                new { Cant = request.Cantidad, UserId = userId, ProdId = productId });

            if (updatedRows == 0)
                throw new NotFoundException("CRT-001", 404, "Producto no encontrado en el carrito.");

            await conn.ExecuteAsync("UPDATE Carritos SET FechaActualizacion = @Fecha WHERE UsuarioId = @UserId",
                new { Fecha = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), UserId = userId });

            return await GetCartResponseInternalAsync(conn, userId);
        }

        public async Task RemoveItemAsync(int userId, int productId)
        {
            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId });

            if (!cartExists.HasValue)
                throw new NotFoundException("CRT-001", 404, "Carrito no encontrado.");

            int rowsAffected = await conn.ExecuteAsync(
                "DELETE FROM CarritoDetalles WHERE UsuarioId = @UserId AND ProductoId = @ProdId",
                new { UserId = userId, ProdId = productId });

            if (rowsAffected == 0)
                throw new NotFoundException("CRT-001", 404, "Producto no encontrado en el carrito.");

            await conn.ExecuteAsync("UPDATE Carritos SET FechaActualizacion = @Fecha WHERE UsuarioId = @UserId",
                new { Fecha = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), UserId = userId });
        }

        public async Task ClearCartAsync(int userId)
        {
            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId });

            if (!cartExists.HasValue)
                throw new NotFoundException("CRT-001", 404, "Carrito no encontrado.");

            if (conn.State == ConnectionState.Closed) conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM CarritoDetalles WHERE UsuarioId = @UserId", new { UserId = userId }, transaction);
                await conn.ExecuteAsync("DELETE FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId }, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw new NotFoundException("CRT-005", 500, "Error interno al vaciar el carrito.");
            }
        }

        
        private async Task<ProductDetailDto> FetchProductFromApiAsync(int productId)
        {
            var client = _clientFactory.CreateClient();
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync($"{_productUrl}/api/products/{productId}");
            }
            catch
            {
                throw new NotFoundException("CRT-005", 500, "Error de comunicación con Products.API.");
            }

            if (!response.IsSuccessStatusCode)
                throw new NotFoundException("CRT-002", 404, "Producto no encontrado.");

            var product = await response.Content.ReadFromJsonAsync<ProductDetailDto>();
            if (product == null)
                throw new NotFoundException("CRT-002", 404, "Producto no encontrado.");

            return product;
        }

        private async Task ValidateUserExistsAsync(int userId)
        {
            var client = _clientFactory.CreateClient();
            HttpResponseMessage response;

            try
            {
               
                response = await client.GetAsync($"{_usersUrl}/api/users/{userId}");
            }
            catch
            {
                throw new NotFoundException("CRT-005", 500, "Error de comunicación con Users.API.");
            }

            if (!response.IsSuccessStatusCode)
                throw new NotFoundException("CRT-001", 404, $"El usuario con ID {userId} no existe en el sistema.");
        }

        private async Task<CartResponse> GetCartResponseInternalAsync(IDbConnection conn, int userId)
        {
            var cabecera = await conn.QuerySingleAsync<(int UsuarioId, string FechaActualizacion)>(
                "SELECT UsuarioId, FechaActualizacion FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId });

            var items = await conn.QueryAsync<CartItemResponse>(
                "SELECT ProductoId, Cantidad FROM CarritoDetalles WHERE UsuarioId = @UserId", new { UserId = userId });

            return new CartResponse
            {
                UsuarioId = cabecera.UsuarioId,
                FechaActualizacion = cabecera.FechaActualizacion,
                Items = items.ToList()
            };
        }
    }
}