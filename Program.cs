using Products.API.Database;
using Products.API.Extensions;
using Products.API.ExceptionHandlers;
using Products.API.Repositories;
using Products.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppLogging();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=app.db";

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddSingleton<ProductService>();

builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks();

var app = builder.Build();

DatabaseInitializer.Initialize(connectionString);

app.UseExceptionHandler();

app.UseCustomRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
