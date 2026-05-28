using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Orders.Api.DTOs;
using Orders.Api.Exceptions;
using Orders.Api.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Orders.Api.Services
{
    public class OrderService : IOrderService
    {
        private readonly string _connectionString;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _cartUrl;
        private readonly string _productUrl;
        private readonly string _notificationUrl;
        private readonly string _usersUrl;

        public OrderService(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db";
            _clientFactory = clientFactory;
            _cartUrl = configuration.GetValue<string>("CartServiceUrl") ?? "";
            _productUrl = configuration.GetValue<string>("ProductServiceUrl") ?? "";
            _notificationUrl = configuration.GetValue<string>("NotificationServiceUrl") ?? "";
            _usersUrl = configuration["UserServiceUrl"] ?? "https://localhost:7058";
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<IEnumerable<OrderResponse>> GetAllAsync(int? usuarioId)
        {
            using var conn = CreateConnection();
            string sql = "SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM Ordenes";

            IEnumerable<Order> ordenes;

            if (usuarioId.HasValue)
            {
                sql += " WHERE UsuarioId = @UsuarioId";
                ordenes = await conn.QueryAsync<Order>(sql, new { UsuarioId = usuarioId.Value });
            }
            else
            {
                ordenes = await conn.QueryAsync<Order>(sql);
            }

            var responses = new List<OrderResponse>();

            foreach (var ord in ordenes)
            {
                var response = new OrderResponse
                {
                    Id = ord.Id,
                    UsuarioId = ord.UsuarioId,
                    Total = ord.Total,
                    Estado = ord.Estado,
                    FechaCreacion = ord.FechaCreacion
                };

                var detalles = await conn.QueryAsync<OrderItemResponse>(
                    "SELECT ProductoId, Cantidad, PrecioUnitario FROM OrdenDetalles WHERE OrdenId = @OrdenId",
                    new { OrdenId = ord.Id });

                response.Items = detalles.ToList();
                responses.Add(response);
            }

            return responses;
        }

        public async Task<OrderResponse> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();

            var ord = await conn.QueryFirstOrDefaultAsync<Order>(
                "SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM Ordenes WHERE Id = @Id",
                new { Id = id });

            if (ord == null)
                throw new NotFoundException("ORD-001", 404, "Orden no encontrada.");

            var response = new OrderResponse
            {
                Id = ord.Id,
                UsuarioId = ord.UsuarioId,
                Total = ord.Total,
                Estado = ord.Estado,
                FechaCreacion = ord.FechaCreacion
            };

            var detalles = await conn.QueryAsync<OrderItemResponse>(
                "SELECT ProductoId, Cantidad, PrecioUnitario FROM OrdenDetalles WHERE OrdenId = @OrdenId",
                new { OrdenId = ord.Id });

            response.Items = detalles.ToList();

            return response;
        }

        public async Task<bool> HasOrdersAsync(int productoId)
        {
            using var conn = CreateConnection();
            if (conn.State == ConnectionState.Closed) conn.Open();

            
            string sql = "SELECT COUNT(1) FROM OrdenDetalles WHERE ProductoId = @ProductoId;";
            int conteo = await conn.ExecuteScalarAsync<int>(sql, new { ProductoId = productoId });

            return conteo > 0;
        }

        //  POST 
        public async Task<OrderResponse> CreateAsync(OrderRequest request)
        {
            var client = _clientFactory.CreateClient();

           
            await ValidateUserExistsAsync(request.UsuarioId);

            
            HttpResponseMessage cartRes;
            try
            {
                cartRes = await client.GetAsync($"{_cartUrl}/api/cart/{request.UsuarioId}");
            }
            catch
            {
                throw new NotFoundException("ORD-007", 500, "Error de comunicación con Cart.API al recuperar el carrito.");
            }

            if (!cartRes.IsSuccessStatusCode)
                throw new NotFoundException("ORD-002", 404, $"No se encontró un carrito activo para el usuario {request.UsuarioId}.");

            
            var carrito = await cartRes.Content.ReadFromJsonAsync<CartResponse>();

            if (carrito == null || carrito.Items == null || !carrito.Items.Any())
                throw new NotFoundException("ORD-002", 400, "El carrito del usuario está vacío. No se puede generar una orden.");

            decimal totalAmount = 0;
            var productosActualizados = new List<(ProductDetailDto Prod, int CantidadAComprar)>();
            var detallesParaGuardar = new List<OrderItemResponse>();

            
            foreach (var item in carrito.Items)
            {
                var productRes = await client.GetAsync($"{_productUrl}/api/products/{item.ProductoId}");
                if (!productRes.IsSuccessStatusCode)
                    throw new NotFoundException("ORD-004", 404, $"Producto {item.ProductoId} no encontrado al crear la orden.");

                var product = await productRes.Content.ReadFromJsonAsync<ProductDetailDto>();
                if (product == null)
                    throw new NotFoundException("ORD-004", 404, "Producto no encontrado al crear la orden.");

                if (product.Stock < item.Cantidad)
                    throw new NotFoundException("ORD-005", 422, $"Stock insuficiente para uno o más productos. Producto: {product.Name}");

                totalAmount += (product.Price * item.Cantidad);
                productosActualizados.Add((product, item.Cantidad));

                detallesParaGuardar.Add(new OrderItemResponse
                {
                    ProductoId = product.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = product.Price
                });
            }

            int nuevaOrdenId;
            var fechaActual = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            
            using var conn = CreateConnection();
            if (conn.State == ConnectionState.Closed) conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                string sqlOrden = @"
                    INSERT INTO Ordenes (UsuarioId, Total, Estado, FechaCreacion) 
                    VALUES (@UsuarioId, @Total, 'Pendiente', @FechaCreacion);";

                await conn.ExecuteAsync(sqlOrden, new
                {
                    UsuarioId = request.UsuarioId,
                    Total = totalAmount,
                    FechaCreacion = fechaActual
                }, transaction);

                nuevaOrdenId = await conn.QuerySingleAsync<int>("SELECT last_insert_rowid();", transaction: transaction);

                string sqlDetalle = @"
                    INSERT INTO OrdenDetalles (OrdenId, ProductoId, Cantidad, PrecioUnitario) 
                    VALUES (@OrdenId, @ProductoId, @Cantidad, @PrecioUnitario);";

                foreach (var detalle in detallesParaGuardar)
                {
                    await conn.ExecuteAsync(sqlDetalle, new
                    {
                        OrdenId = nuevaOrdenId,
                        ProductoId = detalle.ProductoId,
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario
                    }, transaction);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new NotFoundException("ORD-007", 500, $"Error real en base de datos: {ex.Message} -> {ex.InnerException?.Message}");
            }

            
            foreach (var prodInfo in productosActualizados)
            {
                int stockRestante = prodInfo.Prod.Stock - prodInfo.CantidadAComprar;

                var updateStockRes = await client.PutAsJsonAsync($"{_productUrl}/api/products/{prodInfo.Prod.Id}/stock", stockRestante);

                if (!updateStockRes.IsSuccessStatusCode)
                    throw new NotFoundException("ORD-007", 500, $"Error interno. No se pudo actualizar el stock del producto {prodInfo.Prod.Id}.");
            }

            
            await client.DeleteAsync($"{_cartUrl}/api/cart/{request.UsuarioId}");

            
            var dataNotificacion = new
            {
                UsuarioId = request.UsuarioId,
                Mensaje = $"Su orden #{nuevaOrdenId} fue confirmada.",
                Tipo = "Email"
            };

            var respuestaNotification = await client.PostAsJsonAsync($"{_notificationUrl}/api/notifications/send", dataNotificacion);

            if (!respuestaNotification.IsSuccessStatusCode)
            {
                var contenidoError = await respuestaNotification.Content.ReadAsStringAsync();
                throw new NotFoundException("ORD-007", 500, $"Falla al notificar. Código HTTP: {respuestaNotification.StatusCode} -> Cuerpo: {contenidoError}");
            }

            return await GetByIdAsync(nuevaOrdenId);
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

        public async Task<OrderResponse> UpdateStatusAsync(int id, string nuevoEstado)
        {
            var ordenActual = await GetByIdAsync(id);

            if (ordenActual.Estado == "Cancelada" || ordenActual.Estado == "Confirmada")
                throw new NotFoundException("ORD-006", 409, "El estado de la orden no puede ser modificado.");

            using var conn = CreateConnection();
            await conn.ExecuteAsync("UPDATE Ordenes SET Estado = @Estado WHERE Id = @Id", new { Estado = nuevoEstado, Id = id });

            return await GetByIdAsync(id);
        }
    }
}