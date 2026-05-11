Agregar: using Products.API.HealthChecks;
Reemplazar por: builder.Services.AddHealthChecks()
    .AddCheck<SqliteHealthCheck>("sqlite")
    .AddCheck<ApiHealthCheck>("api");
Reemplazar por: app.MapHealthChecks("/health");

app.MapHealthChecks("/health/live");

app.MapHealthChecks("/health/ready");
Probar: https://localhost:xxxx/health y deberia responder Healthy
