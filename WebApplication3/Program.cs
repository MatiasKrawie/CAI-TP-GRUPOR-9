using Microsoft.Data.Sqlite;
using Dapper;
using WebApplication1;
using Serilog;

//ESTE ES EL PROYECTO DE PRODUCTO

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE SERILOG ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console() // Muestra logs en la ventanita negra
    .WriteTo.File("logs/log-productos-.txt", rollingInterval: RollingInterval.Day) // Guarda en archivos por día
    .CreateLogger();

builder.Host.UseSerilog(); // Le decimos a la app que use Serilog
// --------------------------------

builder.Services.AddHttpClient();
// ... resto de tus servicios (Swagger, etc)


builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//hellcheck
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

// Crear la base de datos automáticamente al inicio
using (var connection = new SqliteConnection("Data Source=products.db"))
{
    connection.Open();
    connection.Execute(@"
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Description TEXT,
            Price REAL NOT NULL,
            Stock INTEGER NOT NULL
        );");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// RUTA PARA VER PRODUCTOS
app.MapGet("/products", async () =>
{
    using var connection = new SqliteConnection("Data Source=products.db");
    var products = await connection.QueryAsync<Product>("SELECT * FROM Products");
    return Results.Ok(products);
});

// RUTA PARA CREAR UN PRODUCTO
app.MapPost("/products", async (Product product) =>
{
    using var connection = new SqliteConnection("Data Source=products.db");
    var sql = @"INSERT INTO Products (Name, Description, Price, Stock) 
                VALUES (@Name, @Description, @Price, @Stock);
                SELECT last_insert_rowid();"; // <--- Esto recupera el ID real

    var id = await connection.QuerySingleAsync<int>(sql, product);
    product.Id = id;

    return Results.Created($"/products/{product.Id}", product);
});

// ESTE ES PARA ERRORES EN ID
app.MapGet("/products/{id}", async (int id) =>
{
    Log.Information("Consultando producto con ID: {Id}", id); // <--- LOG DE INFO

    using var connection = new SqliteConnection("Data Source=products.db");
    var product = await connection.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE Id = @Id", new { Id = id });

    if (product == null)
    {
        Log.Warning("Producto {Id} no encontrado", id); // <--- LOG DE ADVERTENCIA
        throw new NotFoundException("PROD_001", $"El producto con ID {id} no existe.");
    }

    return Results.Ok(product);
});
app.MapPatch("/products/{id}/reduce-stock", async (int id, int quantity) =>
{
    using var connection = new SqliteConnection("Data Source=products.db");
    var affectedRows = await connection.ExecuteAsync(
        "UPDATE Products SET Stock = Stock - @Qty WHERE Id = @Id AND Stock >= @Qty",
        new { Qty = quantity, Id = id });

    return affectedRows == 0 ? Results.BadRequest("Stock insuficiente o producto no encontrado") : Results.Ok();
});



app.MapHealthChecks("/health");

app.Run();