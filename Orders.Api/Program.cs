using Microsoft.Data.Sqlite;
using Dapper;
using Orders.Api;

var builder = WebApplication.CreateBuilder(args);

// 1. REGISTRAR LOS SERVICIOS NECESARIOS
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Registramos nuestro protector global de errores
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// 2. CONFIGURAR EL MIDDLEWARE Y SWAGGER
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandler(); // Activa el uso del ExceptionHandler

// 3. CREAR LA BASE DE DATOS DE ÓRDENES
using (var connection = new SqliteConnection("Data Source=orders.db"))
{
    connection.Open();
    connection.Execute(@"
        CREATE TABLE IF NOT EXISTS Orders (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            TotalAmount DECIMAL NOT NULL,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
        )");
}

// 4. ENDPOINT: PROCESAR UNA COMPRA (Checkout con Precios y Stock Reales)
app.MapPost("/orders", async (OrderRequest request, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    var client = clientFactory.CreateClient();
    var cartUrl = config.GetValue<string>("CartServiceUrl") ?? "";
    var productUrl = config.GetValue<string>("ProductServiceUrl") ?? "";

    // Paso A: Consultar el carrito del usuario (Llamada a Cart API)
    var cartRes = await client.GetAsync($"{cartUrl}/cart/{request.UserId}");
    if (!cartRes.IsSuccessStatusCode)
    {
        throw new NotFoundException("ORDER-001", $"No se pudo obtener el carrito del usuario {request.UserId}.");
    }

    var cartItems = await cartRes.Content.ReadFromJsonAsync<List<CartItemDto>>();
    if (cartItems == null || !cartItems.Any())
    {
        throw new NotFoundException("ORDER-002", $"El carrito del usuario {request.UserId} está vacío.");
    }

    // --- ¡NUEVO! Paso B: Calcular el monto total consultando a Products API ---
    decimal totalAmount = 0;
    foreach (var item in cartItems)
    {
        // Llamamos a Products API para buscar el precio actual y real del producto
        var productRes = await client.GetAsync($"{productUrl}/products/{item.ProductId}");

        if (!productRes.IsSuccessStatusCode)
        {
            throw new NotFoundException("ORDER-005", $"El producto {item.ProductId} en el carrito ya no existe en el catálogo.");
        }

        // Leemos el producto dinámicamente para extraer su precio
        var product = await productRes.Content.ReadFromJsonAsync<ProductPriceDto>();

        if (product == null)
        {
            throw new NotFoundException("ORDER-006", $"No se pudieron leer los detalles del producto {item.ProductId}.");
        }

        // Sumamos al total: Precio Real Fijo x Cantidad
        totalAmount += (product.Price * item.Quantity);
    }

    // Paso C.1: Registrar la Orden en nuestra base de datos local de Órdenes
    using var connection = new SqliteConnection("Data Source=orders.db");
    var orderId = await connection.QuerySingleAsync<int>(@"
        INSERT INTO Orders (UserId, TotalAmount) 
        VALUES (@UserId, @TotalAmount);
        SELECT last_insert_rowid();",
        new { UserId = request.UserId, TotalAmount = totalAmount });

    // Paso C.2: Descontar el stock real en Products API
    foreach (var item in cartItems)
    {
        var stockRes = await client.PutAsJsonAsync($"{productUrl}/products/{item.ProductId}/reduce-stock", new { Quantity = item.Quantity });
        if (!stockRes.IsSuccessStatusCode)
        {
            var errorText = await stockRes.Content.ReadAsStringAsync();
            throw new NotFoundException("ORDER-004", $"No se pudo descontar stock para el producto {item.ProductId}. Detalle: {errorText}");
        }
    }

    // Paso D: Mandar a vaciar el carrito automáticamente
    var deleteRes = await client.DeleteAsync($"{cartUrl}/cart/{request.UserId}/clear");
    if (!deleteRes.IsSuccessStatusCode)
    {
        throw new NotFoundException("ORDER-003", $"La orden se creó pero no se pudo vaciar el carrito.");
    }

    // Retornamos la orden creada de forma prolija
    return Results.Json(new
    {
        Message = "¡Compra realizada con éxito con precios reales, stock descontado y carrito vaciado!",
        OrderId = orderId,
        UserId = request.UserId,
        Total = totalAmount
    }, statusCode: 201);
});
app.Run();

// --- DTO AUXILIAR PARA RECIBIR LA PETICIÓN ---
public class OrderRequest
{
    public int UserId { get; set; }
}
// DTO para mapear el precio real que nos devuelve la API de Productos
public class ProductPriceDto
{
    public int Id { get; set; }
    public decimal Price { get; set; }
}