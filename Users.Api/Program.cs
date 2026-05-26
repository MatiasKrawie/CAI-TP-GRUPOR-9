using Microsoft.Data.Sqlite;
using Dapper;
using Users.Api; // Asegurate que coincida con tu namespace
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE SERVICIOS (Antes del Build) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// --- 2. CONFIGURACIÓN DEL MIDDLEWARE (Después del Build) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(_ => { }); // Activador de errores prolijos

// --- 3. INICIALIZACIÓN DE DB (Solo una vez) ---
using (var connection = new SqliteConnection("Data Source=users.db"))
{
    connection.Open();
    connection.Execute(@"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Email TEXT NOT NULL UNIQUE,
            IsActive INTEGER DEFAULT 1
        )");
}

// --- 4. AQUÍ ESCRIBÍS LOS ENDPOINTS ---

// GET: Obtener usuario por ID
app.MapGet("/users/{id}", async (int id) =>
{
    using var connection = new SqliteConnection("Data Source=users.db");
    var user = await connection.QueryFirstOrDefaultAsync<User>(
        "SELECT * FROM Users WHERE Id = @Id", new { Id = id });

    if (user is null)
        throw new NotFoundException("USR-001", $"El usuario con ID {id} no existe.");

    return Results.Ok(user);
});

// POST: Crear usuario
app.MapPost("/users", async (User user) =>
{
    using var connection = new SqliteConnection("Data Source=users.db");
    var id = await connection.QuerySingleAsync<int>(
        @"INSERT INTO Users (Name, Email) VALUES (@Name, @Email); 
          SELECT last_insert_rowid();",
        user);

    user.Id = id;
    return Results.Created($"/users/{id}", user);
});

// --- 5. FIN DEL ARCHIVO ---
app.Run();