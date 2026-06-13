using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Notifications.Api.ExceptionsHandlers;
using Notifications.Api.Services;
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
    .Enrich.WithProperty("Servicio", "Notifications.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{Endpoint}] [CorrelationId: {CorrelationId}] [ErrorCode: {ErrorCode}] - {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/log-notifications-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Soporte nativo para llamadas salientes con trazabilidad
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpClient("UsersClient")
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

builder.Services.AddScoped<INotificationService, NotificationService>();

// Manejo nativo de excepciones
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

// 2. CONFIGURACIÓN DE SWAGGER
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

// 3. REGISTRO DE HEALTH CHECKS PROFESIONALES
builder.Services.AddHealthChecks()
    .AddCheck<ApiStatusCheck>("api-status", tags: new[] { "live" })
    .AddCheck<SqliteHealthCheck>("database-check", tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. MIDDLEWARE DE TRAZABILIDAD
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

// El manejo de excepciones debe ir después del middleware de trazabilidad
app.UseExceptionHandler();

app.MapControllers();

// 5. MAPEO DE ENDPOINTS DE SALUD
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse });
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = (check) => check.Tags.Contains("live"), ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = (check) => check.Tags.Contains("ready"), ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse });

// Inicialización de Base de Datos
using (var scope = app.Services.CreateScope())
{
    try { new Notifications.Api.Services.DatabaseInitializer(scope.ServiceProvider.GetRequiredService<IConfiguration>()).Initialize(); }
    catch (Exception ex) { Log.Fatal(ex, "Error crítico iniciando la base de datos de Notificaciones."); }
}

app.Run();