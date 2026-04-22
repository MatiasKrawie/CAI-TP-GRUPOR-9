using WebApplication3.Extensions;

public partial class Program
{

    private static void Main(string[] args)

    {


        var builder = WebApplication.CreateBuilder(args);


        // Reemplaza el logging por defecto con Serilogbuilder.Host.UseSerilog();


        //  Swaggerbuilder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();


        var app = builder.Build();


        // Swagger UIif (app.Environment.IsDevelopment())

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