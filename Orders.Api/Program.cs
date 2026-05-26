using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Api.ExceptionsHandlers;
using Orders.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-orders-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddTransient<DatabaseInitializer>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<ApiStatusCheck>("api-status")
    .AddCheck<SqliteHealthCheck>("sqlite-db");

builder.Services.AddHealthChecksUI(setup =>
{
    setup.AddHealthCheckEndpoint("API de Órdenes", "/health");
    setup.SetEvaluationTimeInSeconds(5);
}).AddInMemoryStorage(); 

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    initializer.Initialize();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

app.Run();