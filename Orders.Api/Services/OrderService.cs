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

        // 🚀 El TypeHandler garantiza que cuando Dapper LEE un string de SQLite, lo transforme en Guid para tus DTOs automáticamente
        static OrderService()
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

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

        public async Task<IEnumerable<OrderResponse>> GetAllAsync(Guid? usuarioId)
        {
            if (usuarioId.HasValue)
            {
                await ValidateUserExistsAsync(usuarioId.Value);
            }

            using var conn = CreateConnection();
            string sql = "SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM Ordenes";
            IEnumerable<Order> ordenes;

            if (usuarioId.HasValue)
            {
                sql += " WHERE UsuarioId = @UsuarioId";
                ordenes = await conn.QueryAsync<Order>(sql, new { UsuarioId = usuarioId.Value.ToString() });
            }
            else
            {
                ordenes = await conn.QueryAsync<Order>(sql);
            }
            if (ordenes == null || !ordenes.Any())
            {
                throw new NotFoundException("ORD-001", "Orden no encontrada.");
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
                    new { OrdenId = ord.Id.ToString() });

                response.Items = detalles.ToList();
                responses.Add(response);
            }

            return responses;
        }

        public async Task<OrderResponse> GetByIdAsync(Guid id)
        {
            using var conn = CreateConnection();

            var ord = await conn.QueryFirstOrDefaultAsync<Order>(
                "SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM Ordenes WHERE Id = @Id",
                new { Id = id.ToString() });

            if (ord == null)
                throw new NotFoundException("ORD-001", "La orden solicitada no existe.");

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
                new { OrdenId = id.ToString() });

            response.Items = detalles.ToList();

            return response;
        }

        public async Task<bool> HasOrdersAsync(Guid productoId)
        {
            using var conn = CreateConnection();
            string sql = "SELECT COUNT(1) FROM OrdenDetalles WHERE ProductoId = @ProductoId;";
            int conteo = await conn.ExecuteScalarAsync<int>(sql, new { ProductoId = productoId.ToString() });

            return conteo > 0;
        }

        public async Task<OrderResponse> CreateAsync(OrderRequest request)
        {
            await ValidateUserExistsAsync(request.UsuarioId);

            var cartClient = _clientFactory.CreateClient("CartClient");
            HttpResponseMessage cartRes;
            try
            {
                cartRes = await cartClient.GetAsync($"{_cartUrl}/api/cart/{request.UsuarioId}");
            }
            catch (Exception ex)
            {
                throw new BusinessRuleException("ORD-007", $"Error de comunicación de red con Cart.API: {ex.Message}", 500);
            }

            if (!cartRes.IsSuccessStatusCode)
                throw new NotFoundException("ORD-002", $"No se encontró un carrito activo para el usuario {request.UsuarioId}.");

            var carrito = await cartRes.Content.ReadFromJsonAsync<CartResponse>();

            if (carrito == null || carrito.Items == null || !carrito.Items.Any())
                throw new ValidationException("ORD-003", "El carrito del usuario está vacío. No se puede generar una orden.");

            decimal totalAmount = 0;
            var productosActualizados = new List<(ProductDetailDto Prod, int CantidadAComprar)>();
            var detallesParaGuardar = new List<OrderItemResponse>();

            var productClient = _clientFactory.CreateClient("ProductsClient");

            foreach (var item in carrito.Items)
            {
                var productRes = await productClient.GetAsync($"{_productUrl}/api/products/{item.ProductoId}");
                if (!productRes.IsSuccessStatusCode)
                    throw new NotFoundException("ORD-004", $"Producto {item.ProductoId} no encontrado en el catálogo maestro.");

                var product = await productRes.Content.ReadFromJsonAsync<ProductDetailDto>();
                if (product == null)
                    throw new NotFoundException("ORD-004", "Error al deserializar la información del producto.");

                if (product.Stock < item.Cantidad)
                    throw new BusinessRuleException("ORD-005", $"Stock insuficiente en el inventario para el producto: {product.Name}");

                totalAmount += (decimal)(product.Price * item.Cantidad);
                productosActualizados.Add((product, item.Cantidad));

                detallesParaGuardar.Add(new OrderItemResponse
                {
                    ProductoId = product.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = (decimal)product.Price
                });
            }

            Guid nuevaOrdenId = Guid.NewGuid();
            var fechaActual = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            using var conn = CreateConnection();
            conn.Open(); 
            using var transaction = conn.BeginTransaction();

            try
            {
                string sqlOrden = @"
                    INSERT INTO Ordenes (Id, UsuarioId, Total, Estado, FechaCreacion) 
                    VALUES (@Id, @UsuarioId, @Total, 'Pendiente', @FechaCreacion);";

                await conn.ExecuteAsync(sqlOrden, new
                {
                    Id = nuevaOrdenId.ToString(),
                    UsuarioId = request.UsuarioId.ToString(),
                    Total = totalAmount,
                    FechaCreacion = fechaActual
                }, transaction);

                string sqlDetalle = @"
                    INSERT INTO OrdenDetalles (OrdenId, ProductoId, Cantidad, PrecioUnitario) 
                    VALUES (@OrdenId, @ProductoId, @Cantidad, @PrecioUnitario);";

                foreach (var detalle in detallesParaGuardar)
                {
                    await conn.ExecuteAsync(sqlDetalle, new
                    {
                        OrdenId = nuevaOrdenId.ToString(),
                        ProductoId = detalle.ProductoId.ToString(),
                        Cantidad = detalle.Cantidad,
                        PrecioUnitario = detalle.PrecioUnitario
                    }, transaction);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new BusinessRuleException("ORD-007", $"Error transaccional en la base de datos de Órdenes: {ex.Message}", 500);
            }

            // Actualización de stock 
            foreach (var prodInfo in productosActualizados)
            {
                int stockRestante = prodInfo.Prod.Stock - prodInfo.CantidadAComprar;
                var updateStockRes = await productClient.PatchAsJsonAsync(
                    $"{_productUrl}/api/products/{prodInfo.Prod.Id}/stock",
                    new { NuevoStock = stockRestante }
                );

                if (!updateStockRes.IsSuccessStatusCode)
                    throw new BusinessRuleException("ORD-007", $"Falla crítica al intentar descontar stock del producto {prodInfo.Prod.Id}.", 500);
            }

            await cartClient.DeleteAsync($"{_cartUrl}/api/cart/{request.UsuarioId}");

            var genericClient = _clientFactory.CreateClient();
            var dataNotificacion = new
            {
                UsuarioId = request.UsuarioId,
                Mensaje = $"Su orden #{nuevaOrdenId} fue confirmada de manera exitosa.",
                Tipo = "Email"
            };

            try
            {
                await genericClient.PostAsJsonAsync($"{_notificationUrl}/api/notifications/send", dataNotificacion);
            }
            catch
            {
                Serilog.Log.Warning("No se pudo enviar la notificación saliente para la orden {OrdenId}", nuevaOrdenId);
            }

            return await GetByIdAsync(nuevaOrdenId);
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
                throw new BusinessRuleException("ORD-007", $"Falla de comunicación de red con Users.API: {ex.Message}", 500);
            }

            if (!response.IsSuccessStatusCode)
                throw new NotFoundException("ORD-008", $"El usuario con ID {userId} no existe en los registros maestros.");
        }

        public async Task<OrderResponse> UpdateStatusAsync(Guid id, string nuevoEstado)
        {
            var ordenActual = await GetByIdAsync(id);
            string estadoViejo = ordenActual.Estado;

            var estadosValidos = new List<string> { "Pendiente", "Confirmada", "Cancelada", "Entregada" };

            if (!estadosValidos.Contains(nuevoEstado))
            {
                throw new ValidationException("ORD-006", $"El estado '{nuevoEstado}' no es válido. Permitidos: Pendiente, Confirmada, Cancelada, Entregada.");
            }

            if (estadoViejo == nuevoEstado)
            {
                return ordenActual;
            }

            bool esTransicionValida = false;
            string mensajeErrorCustom = "";

            if (estadoViejo == "Pendiente")
            {
                if (nuevoEstado == "Confirmada" || nuevoEstado == "Cancelada") esTransicionValida = true;
                else if (nuevoEstado == "Entregada")
                {
                    mensajeErrorCustom = "Una orden en estado 'Pendiente' no puede pasar directo a 'Entregada' sin ser 'Confirmada'.";
                }
            }
            else if (estadoViejo == "Confirmada")
            {
                if (nuevoEstado == "Entregada") esTransicionValida = true;
                else if (nuevoEstado == "Pendiente" || nuevoEstado == "Cancelada")
                {
                    mensajeErrorCustom = $"Una orden en estado 'Confirmada' no puede mutar a '{nuevoEstado}'.";
                }
            }
            else if (estadoViejo == "Entregada" || estadoViejo == "Cancelada")
            {
                mensajeErrorCustom = $"La orden se encuentra en el estado final '{estadoViejo}' and ya es inmutable.";
            }

            if (!esTransicionValida)
            {
                string errorFinal = string.IsNullOrEmpty(mensajeErrorCustom)
                    ? $"Transición de estado inválida de '{estadoViejo}' a '{nuevoEstado}'."
                    : mensajeErrorCustom;

                throw new BusinessRuleException("ORD-006", errorFinal);
            }

            using var conn = CreateConnection();
            await conn.ExecuteAsync("UPDATE Ordenes SET Estado = @Estado WHERE Id = @Id",
                new { Estado = nuevoEstado, Id = id.ToString() });

            return await GetByIdAsync(id);
        }

        public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
            public override Guid Parse(object value) => Guid.Parse((string)value);
        }
    }
}