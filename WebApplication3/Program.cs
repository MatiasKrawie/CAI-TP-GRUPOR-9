using Serilog;
using WebApplication3.Data;
using WebApplication3.Extensions;   
using WebApplication3.Data.Repositories;


public partial class Program
{

    private static void Main(string[] args)

    {


        var builder = WebApplication.CreateBuilder(args);

        builder.AddAppLogging();
        builder.Host.UseSerilog();




        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        builder.Services.AddTransient<DatabaseInitializer>();
        builder.Services.AddScoped<ProductRepository>();


        var app = builder.Build();


        using (var scope = app.Services.CreateScope())
            scope.ServiceProvider
                .GetRequiredService<DatabaseInitializer>()
                .Initialize();


        app.UseSerilogRequestLogging();




        // Swagger UI

        if (app.Environment.IsDevelopment())

        {

            app.UseSwagger();

            app.UseSwaggerUI();

        }


        app.UseHttpsRedirection();


        app.MapProductEndpoints();



        app.Run();

    }

}