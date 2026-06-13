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

        public async Task<CartResponse> GetByUserIdAsync(Guid userId)
        {
            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId.ToString() });

            if (string.IsNullOrEmpty(cartExists))
                throw new NotFoundException("CRT-001", "Carrito no encontrado para el usuario especificado.");

            return await GetCartResponseInternalAsync(conn, userId);
        }

        public async Task<CartResponse> AddItemAsync(Guid userId, CartItemRequest request)
        {
            if (request.Cantidad <= 0)
                throw new ValidationException("CRT-004", "La cantidad ingresada debe ser mayor a cero.");

            await ValidateUserExistsAsync(userId);

            var product = await FetchProductFromApiAsync(request.ProductoId);

            using var conn = CreateConnection();
            if (conn.State == ConnectionState.Closed) conn.Open();

            var existingItem = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT Cantidad FROM CarritoDetalles WHERE UsuarioId = @UserId AND ProductoId = @ProdId",
                new { UserId = userId.ToString(), ProdId = request.ProductoId.ToString() });

            int nuevaCantidadTotal = request.Cantidad + (existingItem ?? 0);

            if (product.Stock < nuevaCantidadTotal)
                throw new BusinessRuleException("CRT-003", $"Stock insuficiente en el catálogo. Disponible: {product.Stock}, solicitado total: {nuevaCantidadTotal}.", 422);

            using var transaction = conn.BeginTransaction();
            try
            {
                var fechaActual = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                await conn.ExecuteAsync(@"
                    INSERT INTO Carritos (UsuarioId, FechaActualizacion)
                    VALUES (@UserId, @Fecha)
                    ON CONFLICT(UsuarioId) DO UPDATE SET FechaActualizacion = @Fecha;",
                    new { UserId = userId.ToString(), Fecha = fechaActual }, transaction);

                await conn.ExecuteAsync(@"
                    INSERT INTO CarritoDetalles (UsuarioId, ProductoId, Cantidad)
                    VALUES (@UserId, @ProdId, @Cant)
                    ON CONFLICT(UsuarioId, ProductoId) DO UPDATE SET Cantidad = @Cant;",
                    new { UserId = userId.ToString(), ProdId = request.ProductoId.ToString(), Cant = nuevaCantidadTotal }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw new BusinessRuleException("CRT-005", "Error crítico interno al intentar guardar el ítem en la base de datos.", 500);
            }

            return await GetCartResponseInternalAsync(conn, userId);
        }

        public async Task<CartResponse> UpdateItemCantidadAsync(Guid userId, Guid productId, UpdateCantidadRequest request)
        {
            if (request.Cantidad <= 0)
                throw new ValidationException("CRT-004", "La cantidad para actualizar debe ser mayor a cero.");

            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId.ToString() });

            if (string.IsNullOrEmpty(cartExists))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            var product = await FetchProductFromApiAsync(productId);

            if (product.Stock < request.Cantidad)
                throw new BusinessRuleException("CRT-003", $"Stock insuficiente. Máximo disponible en catálogo: {product.Stock}.", 422);

            var updatedRows = await conn.ExecuteAsync(@"
                UPDATE CarritoDetalles SET Cantidad = @Cant  
                WHERE UsuarioId = @UserId AND ProductoId = @ProdId",
                new { Cant = request.Cantidad, UserId = userId.ToString(), ProdId = productId.ToString() });

            if (updatedRows == 0)
                throw new NotFoundException("CRT-006", "El producto especificado no existe adentro de este carrito.");

            await conn.ExecuteAsync("UPDATE Carritos SET FechaActualizacion = @Fecha WHERE UsuarioId = @UserId",
                new { Fecha = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), UserId = userId.ToString() });

            return await GetCartResponseInternalAsync(conn, userId);
        }

        public async Task RemoveItemAsync(Guid userId, Guid productId)
        {
            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId.ToString() });

            if (string.IsNullOrEmpty(cartExists))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            int rowsAffected = await conn.ExecuteAsync(
                "DELETE FROM CarritoDetalles WHERE UsuarioId = @UserId AND ProductoId = @ProdId",
                new { UserId = userId.ToString(), ProdId = productId.ToString() });

            if (rowsAffected == 0)
                throw new NotFoundException("CRT-006", "El producto no se encontraba en el carrito.");

            await conn.ExecuteAsync("UPDATE Carritos SET FechaActualizacion = @Fecha WHERE UsuarioId = @UserId",
                new { Fecha = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), UserId = userId.ToString() });
        }

        public async Task ClearCartAsync(Guid userId)
        {
            await ValidateUserExistsAsync(userId);
            using var conn = CreateConnection();

            var cartExists = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT UsuarioId FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId.ToString() });

            if (string.IsNullOrEmpty(cartExists))
                throw new NotFoundException("CRT-001", "No se puede vaciar un carrito inexistente.");

            if (conn.State == ConnectionState.Closed) conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM CarritoDetalles WHERE UsuarioId = @UserId", new { UserId = userId.ToString() }, transaction);
                await conn.ExecuteAsync("DELETE FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId.ToString() }, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw new BusinessRuleException("CRT-005", "Falla interna al intentar vaciar las tablas del carrito.", 500);
            }
        }

        private async Task<ProductDetailDto> FetchProductFromApiAsync(Guid productId)
        {
            var client = _clientFactory.CreateClient("ProductsClient");
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync($"{_productUrl}/api/products/{productId}");
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException("CRT-007", $"Falla de red al conectar con Products.API: {ex.Message}", 500);
            }

            if (!response.IsSuccessStatusCode)
                throw new NotFoundException("CRT-002", "El producto solicitado no existe en el catálogo maestro.");

            var product = await response.Content.ReadFromJsonAsync<ProductDetailDto>();
            if (product == null)
                throw new BusinessRuleException("CRT-008", "Error al deserializar el catálogo de productos.", 500);

            return product;
        }

        private async Task ValidateUserExistsAsync(Guid userId)
        {
            var client = _clientFactory.CreateClient("UsersClient");
            HttpResponseMessage response;

            try
            {
                response = await client.GetAsync($"{_usersUrl}/api/users/{userId}");
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException("CRT-009", $"Falla de red al conectar con Users.API: {ex.Message}", 500);
            }

            if (!response.IsSuccessStatusCode)
                throw new NotFoundException("CRT-010", $"Acceso denegado. El usuario con ID {userId} no es un usuario válido en el sistema.");
        }

        private async Task<CartResponse> GetCartResponseInternalAsync(IDbConnection conn, Guid userId)
        {
            var cabeceraRow = await conn.QuerySingleAsync<dynamic>(
                "SELECT UsuarioId, FechaActualizacion FROM Carritos WHERE UsuarioId = @UserId", new { UserId = userId.ToString() });

            var itemsRows = await conn.QueryAsync<dynamic>(
                "SELECT ProductoId, Cantidad FROM CarritoDetalles WHERE UsuarioId = @UserId", new { UserId = userId.ToString() });

            var itemsList = new List<CartItemResponse>();

            foreach (var row in itemsRows)
            {
                var prodGuid = Guid.Parse((string)row.ProductoId);

                try
                {
                    var productInfo = await FetchProductFromApiAsync(prodGuid);

                    itemsList.Add(new CartItemResponse
                    {
                        ProductoId = prodGuid,
                        Cantidad = Convert.ToInt32(row.Cantidad),
                        Nombre = productInfo.Name,
                        Precio = (double)productInfo.Price
                    });
                }
                catch (NotFoundException)
                {
                    itemsList.Add(new CartItemResponse
                    {
                        ProductoId = prodGuid,
                        Cantidad = Convert.ToInt32(row.Cantidad),
                        Nombre = "Producto No Disponible",
                        Precio = 0.0
                    });
                }
            }

            return new CartResponse
            {
                UsuarioId = Guid.Parse((string)cabeceraRow.UsuarioId),
                FechaActualizacion = (string)cabeceraRow.FechaActualizacion,
                Items = itemsList
            };
        }
    }
}