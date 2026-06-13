using Serilog;
using System;
using Users.Api.Services;
using Users.Api.ExceptionsHandlers; 
using System.Text.Json;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); 


builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddEndpointsApiExplorer();


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

try 
{
    var initializer = new DatabaseInitializer(app.Configuration);
    initializer.Initialize();
    Log.Information("Base de datos de Usuarios inicializada con éxito.");
}
catch (Exception ex)
{
    Log.Error(ex, "Error crítico al correr el DatabaseInitializer de Usuarios.");
}



app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

try
{
    Log.Information("Iniciando la API de Usuarios");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La API de Usuarios terminó inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}