using Serilog;
using Products.API.Services;
using Products.Api.ExceptionsHandlers; 
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

builder.Services.AddHttpClient("OrdersClient", client =>
{
    var ordersUrl = builder.Configuration["OrdersServiceUrl"] ?? "https://localhost:7040";
    client.BaseAddress = new Uri(ordersUrl);
})
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>(); 

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); 

builder.Services.AddTransient<DatabaseInitializer>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        initializer.Initialize(); 
    }
    catch (Exception ex)
    {
        Log.Error(ex, "No se pudo verificar o inicializar la base de datos de Productos.");
    }
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

try
{
    Log.Information("Iniciando la API de Productos...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La API terminó inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}