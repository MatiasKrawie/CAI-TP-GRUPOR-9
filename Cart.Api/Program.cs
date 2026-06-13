using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;
using Cart.Api.Services;
using Cart.Api.Exceptions; 

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE SERILOG 
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Servicio", "Cart.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{Endpoint}] [CorrelationId: {CorrelationId}] [ErrorCode: {ErrorCode}] - {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-cart-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

//  2. REGISTRO DE HTTP CLIENTS NOMBRADOS 
builder.Services.AddHttpClient("ProductsClient");
builder.Services.AddHttpClient("UsersClient");

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

// 3. CONFIGURACIÓN DE SWAGGER
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 4. REGISTRO DE HEALTH CHECKS CON TAGS
builder.Services.AddHealthChecks()
    .AddCheck("api-status", () => HealthCheckResult.Healthy("API de Carrito operativa"), tags: new[] { "live" })
    .AddCheck("database-check", () => HealthCheckResult.Healthy("Estado de SQLite Carrito OK"), tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var statusCode = 500;
        var errorCode = "SERVER-001";
        var errorMessage = "Ocurrió un error interno indeseado en la API de carrito.";
        var title = "Internal Server Error";
        var type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        var actualException = ex is AggregateException && ex.InnerException != null ? ex.InnerException : ex;

        if (actualException is DomainException domainEx)
        {
            statusCode = domainEx.StatusCode;
            errorCode = domainEx.ErrorCode;
            errorMessage = domainEx.Message;

            title = statusCode switch
            {
                400 => "Bad Request",
                404 => "Not Found",
                422 => "Unprocessable Entity",
                409 => "Conflict",
                _ => "Domain Error"
            };

            type = "https://tools.ietf.org/html/rfc7231#section-6.5";
        }

        using (Serilog.Context.LogContext.PushProperty("ErrorCode", errorCode))
        {
            Log.Error(actualException, "Error capturado en el middleware global de Carrito. Código Error: {ErrorCode}", errorCode);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var errorResponse = new
        {
            type = type,
            title = title,
            status = statusCode,
            detail = "Ocurrió un problema al procesar la solicitud del carrito.",
            instance = context.Request.Path.Value,
            errorCode = errorCode,
            errorMessage = errorMessage
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
});

app.MapControllers();

// 5. MAPEO DE ENDPOINTS DE SALUD
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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var configuration = services.GetRequiredService<IConfiguration>();

       
        var initializer = new Cart.Api.Services.DatabaseInitializer(configuration);

        
        initializer.Initialize();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Falla crítica al intentar correr el DatabaseInitializer del Carrito.");
    }
}

app.Run();