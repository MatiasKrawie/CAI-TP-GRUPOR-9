using Serilog;
using WebApplication3.Extensions;

public partial class Program
{

    private static void Main(string[] args)

    {


        var builder = WebApplication.CreateBuilder(args);

        builder.AddAppLogging();
        // Reemplaza el logging por defecto con Serilogbuilder.Host.UseSerilog();


        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=app.db";

        //  Swaggerbuilder.Services.AddEndpointsApiExplorer();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        var app = builder.Build();

        app.UseSerilogRequestLogging();


        // Swagger UI

        if (app.Environment.IsDevelopment())

        {

            app.UseSwagger();

            app.UseSwaggerUI();

        }


        app.UseHttpsRedirection();


        // Endpointsapp.MapItemEndpoints();
        app.MapItemEndpoints();


        app.Run();

    }

}