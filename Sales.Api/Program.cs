using Microsoft.Data.Sqlite;
using Dapper;
using Sales.Api;

using Serilog; // <--- No te olvides de este using

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE SERILOG PARA VENTAS ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-ventas-.txt", rollingInterval: RollingInterval.Day) // Nombre distinto
    .CreateLogger();

builder.Host.UseSerilog();
// --------------------------------------------

// ... (tus otros servicios: AddHttpClient, AddEndpointsApiExplorer, etc.)

builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Crear la base de datos de ventas (sales.db)
using (var connection = new SqliteConnection("Data Source=sales.db"))
{
    connection.Open();
    connection.Execute(@"
        CREATE TABLE IF NOT EXISTS Sales (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ProductId INTEGER NOT NULL,
            Quantity INTEGER NOT NULL,
            SaleDate TEXT NOT NULL
        );");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// RUTA PARA REGISTRAR UNA VENTA
// Agregamos "IConfiguration config" aquí para poder leer el JSON
app.MapPost("/sales", async (Sale sale, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    // 1. LEER LA URL DEL JSON
    string baseUrl = config.GetValue<string>("ProductServiceUrl") ?? "";

    Log.Information("Iniciando venta. Producto: {ProductId}", sale.ProductId);
    var client = clientFactory.CreateClient();

    // 2. USAR LA VARIABLE baseUrl (ya no el link largo)
    var productCheck = await client.GetAsync($"{baseUrl}/products/{sale.ProductId}");

    if (!productCheck.IsSuccessStatusCode)
    {
        Log.Warning("Producto {ProductId} no existe", sale.ProductId);
        throw new NotFoundException("ORD-001", "El producto no existe.");
    }

    // 3. USAR LA VARIABLE TAMBIÉN AQUÍ
    var response = await client.PatchAsync($"{baseUrl}/products/{sale.ProductId}/reduce-stock?quantity={sale.Quantity}", null);

    if (!response.IsSuccessStatusCode)
    {
        Log.Error("Sin stock para {ProductId}", sale.ProductId);
        return Results.BadRequest(new { errorCode = "ORD-005", errorMessage = "Sin stock." });
    }

    // Persistencia (Tu código de siempre)
    using var connection = new SqliteConnection("Data Source=sales.db");
    await connection.ExecuteAsync("INSERT INTO Sales (ProductId, Quantity, SaleDate) VALUES (@ProductId, @Quantity, @SaleDate)", sale);

    Log.Information("Venta exitosa");
    return Results.Created($"/sales/{sale.ProductId}", sale);
});
app.UseExceptionHandler(_ => { });
app.MapHealthChecks("/health");

app.Run();