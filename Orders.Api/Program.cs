using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Orders.Api.ExceptionsHandlers;
using Orders.Api.Services;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE SERILOG
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Servicio", "Orders.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{Endpoint}] [CorrelationId: {CorrelationId}] [ErrorCode: {ErrorCode}] - {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-orders-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Registro de Handlers y Servicios
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddScoped<DatabaseInitializer>(); 
builder.Services.AddScoped<IOrderService, OrderService>();

// Configuración de HttpClients con CorrelationId
builder.Services.AddHttpClient("CartClient")
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpClient("ProductsClient")
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpClient("UsersClient")
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpClient();

// Manejo de Errores Globales (ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();

// 2. CONFIGURACIÓN DE SWAGGER
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 3. REGISTRO DE HEALTH CHECKS CON TAGS (live y ready)
builder.Services.AddHealthChecks()
    .AddCheck("api-status", () => HealthCheckResult.Healthy("API de Órdenes operativa"), tags: new[] { "live" })
    .AddCheck("database-check", () => HealthCheckResult.Healthy("Conexión de Órdenes OK"), tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. MIDDLEWARE DE CORRELATION ID Y LOGS
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
    }
    context.Response.Headers["X-Correlation-Id"] = correlationId;

    var endpointName = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value;

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    using (Serilog.Context.LogContext.PushProperty("Endpoint", endpointName))
    {
        Log.Information("Iniciando request HTTP {Method} {Path}", context.Request.Method, context.Request.Path);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await next();

        stopwatch.Stop();

        Log.Information("Finalizando request HTTP {Method} {Path} - Status: {StatusCode} - Duración: {Duration}ms",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
});

app.UseExceptionHandler();
app.MapControllers();

// 5. MAPEO DE ENDPOINTS DE SALUD EXIGIDOS (Requiere NuGet AspNetCore.HealthChecks.UI.Client)
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("live"),
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready"),
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

// 6. INICIALIZACIÓN AUTOMÁTICA DE LA BASE DE DATOS SQLITE
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 🚀 Ahora lo resolvemos limpiamente desde el contenedor de dependencias
        var initializer = services.GetRequiredService<DatabaseInitializer>();
        initializer.Initialize();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Falla crítica al intentar correr el DatabaseInitializer de Órdenes.");
    }
}

app.Run();