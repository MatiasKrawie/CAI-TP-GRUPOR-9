using Microsoft.Extensions.DependencyInjection;
using WebApplication3.Data;


namespace WebApplication3.Extensions
{
    public static class ServicesExtensions
    {

        public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
        {

            services.AddEndpointsApiExplorer(); // descubre los endpoints Minimal API
            services.AddSwaggerGen();           // genera la especificación OpenAPI


            services.AddHealthChecks()
            .AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["database"])
            .AddCheck<ApiStatusCheck>("api-status", tags: ["api"]);
 
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(600); // evalúa cada 10 minutos
            setup.AddHealthCheckEndpoint("MiApi", "/health");
        }).AddInMemoryStorage();

            return services;

        }
    }
}
