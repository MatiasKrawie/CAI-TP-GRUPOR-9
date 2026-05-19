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

// 4. ENDPOINT: PROCESAR UNA COMPRA (Checkout)
app.MapPost("/orders", async (OrderRequest request, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    var client = clientFactory.CreateClient();

    // Paso A: Consultar el carrito del usuario (Llamada a Cart API)
    var cartUrl = config.GetValue<string>("CartServiceUrl") ?? "";
    var cartRes = await client.GetAsync($"{cartUrl}/cart/{request.UserId}");

    if (!cartRes.IsSuccessStatusCode)
    {
        throw new NotFoundException("ORDER-001", $"No se pudo obtener el carrito del usuario {request.UserId}.");
    }

    var cartItems = await cartRes.Content.ReadFromJsonAsync<List<CartItemDto>>();

    // Si el carrito está vacío, no hay nada que comprar
    if (cartItems == null || !cartItems.Any())
    {
        throw new NotFoundException("ORDER-002", $"El carrito del usuario {request.UserId} está vacío.");
    }

    // Paso B: Calcular el monto total hardcodeado por ahora (ejemplo: $100 por ítem)
    // En un sistema real, acá llamarías a Products API para traer los precios reales
    decimal totalAmount = 0;
    foreach (var item in cartItems)
    {
        totalAmount += (100 * item.Quantity);
    }

    // Paso C: Registrar la Orden en nuestra base de datos local
    using var connection = new SqliteConnection("Data Source=orders.db");
    var orderId = await connection.QuerySingleAsync<int>(@"
        INSERT INTO Orders (UserId, TotalAmount) 
        VALUES (@UserId, @TotalAmount);
        SELECT last_insert_rowid();",
        new { UserId = request.UserId, TotalAmount = totalAmount });

    // Paso D: Mandar a vaciar el carrito (Opcional por ahora)
    // Acá iría un HTTP DELETE a la Cart API para limpiar el carrito, lo dejamos para después.

    // Retornamos la orden creada de forma prolija
    return Results.Json(new
    {
        Message = "¡Compra realizada con éxito!",
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