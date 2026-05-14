using Microsoft.Data.Sqlite;
using Dapper;
using Cart.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE SERVICIOS ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // IMPORTANTE: Para llamar a otras APIs
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- 2. MIDDLEWARE ---
app.UseExceptionHandler(_ => { });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- 3. INICIALIZACIÓN DE DB ---
using (var connection = new SqliteConnection("Data Source=cart.db"))
{
    connection.Open();
    connection.Execute(@"
        CREATE TABLE IF NOT EXISTS CartItems (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            ProductId INTEGER NOT NULL,
            Quantity INTEGER NOT NULL
        )");
}

// --- 4. ENDPOINTS ---

// GET: Obtener ítems del carrito por Usuario
app.MapGet("/cart/{userId}", async (int userId) =>
{
    using var connection = new SqliteConnection("Data Source=cart.db");
    var items = await connection.QueryAsync<CartItem>(
        "SELECT * FROM CartItems WHERE UserId = @UserId", new { UserId = userId });
    return Results.Ok(items);
});

// POST: Agregar al carrito (Con validación externa)
app.MapPost("/cart", async (CartItem item, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    var client = clientFactory.CreateClient();

    // 1. Validar que el Producto existe (Llamada a Products API)
    var prodUrl = config.GetValue<string>("ProductServiceUrl") ?? "";
    var prodRes = await client.GetAsync($"{prodUrl}/products/{item.ProductId}");
    if (!prodRes.IsSuccessStatusCode)
        throw new NotFoundException("CART-001", $"El producto {item.ProductId} no existe en el catálogo.");

    // 2. Validar que el Usuario existe (Llamada a Users API)
    var userUrl = config.GetValue<string>("UserServiceUrl") ?? "";
    var userRes = await client.GetAsync($"{userUrl}/users/{item.UserId}");
    if (!userRes.IsSuccessStatusCode)
        throw new NotFoundException("CART-002", $"El usuario {item.UserId} no existe.");

    // 3. Si todo está OK, guardar en la base del carrito
    using var connection = new SqliteConnection("Data Source=cart.db");
    await connection.ExecuteAsync(
        "INSERT INTO CartItems (UserId, ProductId, Quantity) VALUES (@UserId, @ProductId, @Quantity)",
        item);

    return Results.StatusCode(201);
});

// DELETE: Vaciar carrito (Lo usaremos cuando se concrete la compra)
app.MapDelete("/cart/{userId}", async (int userId) =>
{
    using var connection = new SqliteConnection("Data Source=cart.db");
    await connection.ExecuteAsync("DELETE FROM CartItems WHERE UserId = @UserId", new { UserId = userId });
    return Results.NoContent();
});

app.Run();